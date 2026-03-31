using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace ToDoapp.Services;

public interface IHolidayDateResolver
{
    HolidayDateResolution? ResolveHolidayDate(string holidayText, HolidayDateRelation relation, DateTime referenceDate);
}

public interface IHolidayScheduleProvider
{
    Task<HolidayScheduleFetchResult> FetchYearAsync(int year, CancellationToken cancellationToken = default);
}

public sealed record HolidayYearSchedule(int Year, IReadOnlyList<HolidayRange> Ranges);
public sealed record HolidayScheduleFetchResult(HolidayScheduleFetchStatus Status, HolidayYearSchedule? Schedule);

public enum HolidayScheduleFetchStatus
{
    Success = 0,
    NotPublished = 1,
    Failed = 2
}

public sealed record HolidayDateResolution(DateTime DueDate, string CanonicalName, bool UsesOfficialSchedule);

public sealed class HolidayWarmupStatusChangedEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}

public sealed class HolidayCalendarService : IHolidayDateResolver
{
    private static readonly Lazy<HolidayCalendarService> LazyInstance = new(() => new HolidayCalendarService());

    private readonly object _syncRoot = new();
    private readonly IHolidayScheduleProvider _provider;
    private readonly string _cacheFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private Dictionary<int, CachedHolidayYear> _cachedYears;

    public static HolidayCalendarService Instance => LazyInstance.Value;

    public string? LastWarmupStatusMessage { get; private set; }

    public event EventHandler<HolidayWarmupStatusChangedEventArgs>? WarmupStatusChanged;

    public HolidayCalendarService(IHolidayScheduleProvider? provider = null, string? cacheFilePath = null)
    {
        _provider = provider ?? new HolidayCalendarApiProvider();
        _cacheFilePath = cacheFilePath ?? GetDefaultCacheFilePath();
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        _cachedYears = LoadCache();
    }

    public async Task WarmupAsync(DateTime referenceDate, CancellationToken cancellationToken = default)
    {
        var targetYears = new[] { referenceDate.Year, referenceDate.Year + 1 };
        var refreshedYears = new List<int>();
        var cachedYears = new List<int>();
        var notPublishedWithCacheYears = new List<int>();
        var notPublishedWithoutCacheYears = new List<int>();
        var failedWithCacheYears = new List<int>();
        var failedWithoutCacheYears = new List<int>();

        foreach (var year in targetYears.Distinct())
        {
            if (!ShouldRefreshYear(year))
            {
                cachedYears.Add(year);
                continue;
            }

            var hadCache = HasCachedYear(year);

            try
            {
                var fetchResult = await _provider.FetchYearAsync(year, cancellationToken);
                if (fetchResult.Status == HolidayScheduleFetchStatus.NotPublished)
                {
                    TrackFetchFailure(year, hadCache, notPublishedWithCacheYears, notPublishedWithoutCacheYears);
                    continue;
                }

                if (fetchResult.Status != HolidayScheduleFetchStatus.Success || fetchResult.Schedule is null)
                {
                    TrackFetchFailure(year, hadCache, failedWithCacheYears, failedWithoutCacheYears);
                    continue;
                }

                lock (_syncRoot)
                {
                    _cachedYears[year] = CachedHolidayYear.FromSchedule(fetchResult.Schedule, DateTime.UtcNow);
                }

                SaveCache();
                refreshedYears.Add(year);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新节假日日历失败: {ex.Message}");
                TrackFetchFailure(year, hadCache, failedWithCacheYears, failedWithoutCacheYears);
            }
        }

        PublishWarmupStatus(BuildWarmupStatusMessage(
            refreshedYears,
            cachedYears,
            notPublishedWithCacheYears,
            notPublishedWithoutCacheYears,
            failedWithCacheYears,
            failedWithoutCacheYears));
    }

    public HolidayDateResolution? ResolveHolidayDate(string holidayText, HolidayDateRelation relation, DateTime referenceDate)
    {
        if (!HolidayCatalog.TryGetCanonicalName(holidayText, out var canonicalName))
        {
            return null;
        }

        var normalizedReferenceDate = referenceDate.Date;
        var ranges = GetAllRanges(canonicalName);
        var cachedCandidate = ranges
            .Select(range => CalculateDueDate(range, relation))
            .Where(dueDate => dueDate >= normalizedReferenceDate)
            .OrderBy(dueDate => dueDate)
            .FirstOrDefault();

        if (cachedCandidate != default)
        {
            return new HolidayDateResolution(cachedCandidate, canonicalName, true);
        }

        var anchorDate = ResolveFromAnchor(canonicalName, relation, normalizedReferenceDate);
        return anchorDate.HasValue
            ? new HolidayDateResolution(anchorDate.Value, canonicalName, false)
            : null;
    }

    private IEnumerable<HolidayRange> GetAllRanges(string canonicalName)
    {
        lock (_syncRoot)
        {
            return _cachedYears.Values
                .SelectMany(year => year.Ranges)
                .Where(range => string.Equals(range.CanonicalName, canonicalName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(range => range.StartDate)
                .ToArray();
        }
    }

    private static DateTime CalculateDueDate(HolidayRange range, HolidayDateRelation relation)
    {
        return relation switch
        {
            HolidayDateRelation.BeforeHoliday => range.StartDate.AddDays(-1),
            HolidayDateRelation.AfterHoliday => range.EndDate.AddDays(1),
            _ => range.StartDate
        };
    }

    private static DateTime? ResolveFromAnchor(string canonicalName, HolidayDateRelation relation, DateTime referenceDate)
    {
        for (var yearOffset = 0; yearOffset <= 2; yearOffset++)
        {
            var anchorDate = HolidayCatalog.TryGetAnchorDate(canonicalName, referenceDate.Year + yearOffset);
            if (!anchorDate.HasValue)
            {
                continue;
            }

            var dueDate = relation switch
            {
                HolidayDateRelation.BeforeHoliday => anchorDate.Value.AddDays(-1),
                HolidayDateRelation.AfterHoliday => anchorDate.Value.AddDays(1),
                _ => anchorDate.Value
            };

            if (dueDate >= referenceDate)
            {
                return dueDate;
            }
        }

        return null;
    }

    private bool ShouldRefreshYear(int year)
    {
        lock (_syncRoot)
        {
            if (!_cachedYears.TryGetValue(year, out var cachedYear))
            {
                return true;
            }

            return cachedYear.FetchedAtUtc.Date < DateTime.UtcNow.Date;
        }
    }

    private bool HasCachedYear(int year)
    {
        lock (_syncRoot)
        {
            return _cachedYears.ContainsKey(year);
        }
    }

    private Dictionary<int, CachedHolidayYear> LoadCache()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                return [];
            }

            var json = File.ReadAllText(_cacheFilePath);
            var cache = JsonSerializer.Deserialize<HolidayCacheDocument>(json, _jsonOptions);
            return cache?.Years?.ToDictionary(
                       item => item.Year,
                       item => item,
                       EqualityComparer<int>.Default)
                   ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载节假日缓存失败: {ex.Message}");
            return [];
        }
    }

    private void SaveCache()
    {
        try
        {
            var directoryPath = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            HolidayCacheDocument snapshot;
            lock (_syncRoot)
            {
                snapshot = new HolidayCacheDocument
                {
                    Years = _cachedYears.Values.OrderBy(item => item.Year).ToList()
                };
            }

            var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
            File.WriteAllText(_cacheFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存节假日缓存失败: {ex.Message}");
        }
    }

    private static string GetDefaultCacheFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "ToDoApp");
        Directory.CreateDirectory(appFolder);
        return Path.Combine(appFolder, "holiday-calendar-cache.json");
    }

    private void PublishWarmupStatus(string message)
    {
        LastWarmupStatusMessage = message;
        WarmupStatusChanged?.Invoke(this, new HolidayWarmupStatusChangedEventArgs(message));
    }

    private static void TrackFetchFailure(int year, bool hadCache, ICollection<int> failedWithCacheYears, ICollection<int> failedWithoutCacheYears)
    {
        if (hadCache)
        {
            failedWithCacheYears.Add(year);
            return;
        }

        failedWithoutCacheYears.Add(year);
    }

    private static string BuildWarmupStatusMessage(
        IReadOnlyCollection<int> refreshedYears,
        IReadOnlyCollection<int> cachedYears,
        IReadOnlyCollection<int> notPublishedWithCacheYears,
        IReadOnlyCollection<int> notPublishedWithoutCacheYears,
        IReadOnlyCollection<int> failedWithCacheYears,
        IReadOnlyCollection<int> failedWithoutCacheYears)
    {
        if (notPublishedWithoutCacheYears.Count > 0)
        {
            return $"节假日数据暂未发布（{FormatYears(notPublishedWithoutCacheYears)}），已回退节日锚点";
        }

        if (notPublishedWithCacheYears.Count > 0)
        {
            return $"节假日数据暂未发布（{FormatYears(notPublishedWithCacheYears)}），已使用本地缓存";
        }

        if (failedWithoutCacheYears.Count > 0)
        {
            return $"节假日数据联网失败（{FormatYears(failedWithoutCacheYears)}），已回退节日锚点";
        }

        if (failedWithCacheYears.Count > 0)
        {
            return $"节假日数据联网失败（{FormatYears(failedWithCacheYears)}），已使用本地缓存";
        }

        if (refreshedYears.Count > 0)
        {
            return $"节假日数据已联网更新（{FormatYears(refreshedYears)}）";
        }

        if (cachedYears.Count > 0)
        {
            return $"节假日数据已就绪（本地缓存：{FormatYears(cachedYears)}）";
        }

        return "节假日数据已就绪";
    }

    private static string FormatYears(IEnumerable<int> years)
    {
        return string.Join("、", years.OrderBy(year => year));
    }

    private sealed class HolidayCacheDocument
    {
        public List<CachedHolidayYear> Years { get; init; } = [];
    }

    private sealed class CachedHolidayYear
    {
        public int Year { get; init; }

        public DateTime FetchedAtUtc { get; init; }

        public List<HolidayRange> Ranges { get; init; } = [];

        public static CachedHolidayYear FromSchedule(HolidayYearSchedule schedule, DateTime fetchedAtUtc)
        {
            return new CachedHolidayYear
            {
                Year = schedule.Year,
                FetchedAtUtc = fetchedAtUtc,
                Ranges = schedule.Ranges.ToList()
            };
        }
    }
}

public sealed class HolidayCalendarApiProvider : IHolidayScheduleProvider
{
    private const string PublicHolidayType = "public_holiday";
    private readonly HttpClient _httpClient;
    private readonly string _urlTemplate;

    public HolidayCalendarApiProvider(HttpClient? httpClient = null, string? urlTemplate = null)
    {
        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };
        _urlTemplate = urlTemplate ?? "https://unpkg.com/holiday-calendar/data/CN/{0}.min.json";
    }

    public async Task<HolidayScheduleFetchResult> FetchYearAsync(int year, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(string.Format(_urlTemplate, year), cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new HolidayScheduleFetchResult(HolidayScheduleFetchStatus.NotPublished, null);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new HolidayScheduleFetchResult(HolidayScheduleFetchStatus.Failed, null);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var schedule = TryParseYearSchedule(year, document.RootElement);
        return schedule is null
            ? new HolidayScheduleFetchResult(HolidayScheduleFetchStatus.Failed, null)
            : new HolidayScheduleFetchResult(HolidayScheduleFetchStatus.Success, schedule);
    }

    private static HolidayYearSchedule? TryParseYearSchedule(int fallbackYear, JsonElement root)
    {
        var year = root.TryGetProperty("year", out var yearElement) && yearElement.TryGetInt32(out var parsedYear)
            ? parsedYear
            : fallbackYear;

        if (!TryGetHolidayArray(root, out var holidayArray))
        {
            return null;
        }

        var groupedDates = new Dictionary<string, List<DateTime>>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in holidayArray.EnumerateArray())
        {
            if (!TryReadEntry(item, out var date, out var nameCn, out var name, out var type) ||
                !string.Equals(type, PublicHolidayType, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var matchedCanonicalNames = HolidayCatalog.MatchCanonicalNames(nameCn)
                .Concat(HolidayCatalog.MatchCanonicalNames(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var canonicalName in matchedCanonicalNames)
            {
                if (!groupedDates.TryGetValue(canonicalName, out var dates))
                {
                    dates = [];
                    groupedDates[canonicalName] = dates;
                }

                dates.Add(date);
            }
        }

        var ranges = groupedDates
            .SelectMany(pair => BuildRanges(pair.Key, pair.Value))
            .OrderBy(range => range.StartDate)
            .ToArray();

        return new HolidayYearSchedule(year, ranges);
    }

    private static bool TryGetHolidayArray(JsonElement root, out JsonElement holidayArray)
    {
        if (root.TryGetProperty("dates", out holidayArray) && holidayArray.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        if (root.TryGetProperty("holidays", out holidayArray) && holidayArray.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        holidayArray = default;
        return false;
    }

    private static bool TryReadEntry(
        JsonElement item,
        out DateTime date,
        out string nameCn,
        out string name,
        out string type)
    {
        date = default;
        nameCn = string.Empty;
        name = string.Empty;
        type = string.Empty;

        if (!item.TryGetProperty("date", out var dateElement) ||
            !DateTime.TryParse(dateElement.GetString(), out date))
        {
            return false;
        }

        if (item.TryGetProperty("name_cn", out var nameCnElement))
        {
            nameCn = nameCnElement.GetString() ?? string.Empty;
        }

        if (item.TryGetProperty("name", out var nameElement))
        {
            name = nameElement.GetString() ?? string.Empty;
        }

        if (item.TryGetProperty("type", out var typeElement))
        {
            type = typeElement.GetString() ?? string.Empty;
        }

        return true;
    }

    private static IEnumerable<HolidayRange> BuildRanges(string canonicalName, IEnumerable<DateTime> dates)
    {
        var orderedDates = dates
            .Select(date => date.Date)
            .Distinct()
            .OrderBy(date => date)
            .ToArray();

        if (orderedDates.Length == 0)
        {
            yield break;
        }

        var start = orderedDates[0];
        var previous = start;

        for (var index = 1; index < orderedDates.Length; index++)
        {
            var current = orderedDates[index];
            if ((current - previous).Days == 1)
            {
                previous = current;
                continue;
            }

            yield return new HolidayRange(canonicalName, start, previous, HolidayCatalog.GetAliases(canonicalName));
            start = current;
            previous = current;
        }

        yield return new HolidayRange(canonicalName, start, previous, HolidayCatalog.GetAliases(canonicalName));
    }
}
