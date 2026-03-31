using ToDoapp.Services;
using Xunit;

namespace ToDoapp.Tests;

public class HolidayCalendarServiceTests
{
    [Fact]
    public async Task ResolveDueDate_UsesOfficialHolidayRangeWhenCacheIsWarmed()
    {
        using var context = new TemporaryHolidayCache();
        var service = new HolidayCalendarService(new StubHolidayScheduleProvider([
            new HolidayYearSchedule(2026,
            [
                new HolidayRange("清明节", new DateTime(2026, 4, 4), new DateTime(2026, 4, 6), HolidayCatalog.GetAliases("清明节")),
                new HolidayRange("劳动节", new DateTime(2026, 5, 1), new DateTime(2026, 5, 5), HolidayCatalog.GetAliases("劳动节"))
            ])
        ]), context.FilePath);

        await service.WarmupAsync(new DateTime(2026, 4, 1));

        Assert.Equal(new DateTime(2026, 4, 7), service.ResolveHolidayDate("清明节", HolidayDateRelation.AfterHoliday, new DateTime(2026, 4, 1))?.DueDate);
        Assert.Equal(new DateTime(2026, 4, 30), service.ResolveHolidayDate("五一节", HolidayDateRelation.BeforeHoliday, new DateTime(2026, 4, 1))?.DueDate);
        Assert.Equal(new DateTime(2026, 5, 1), service.ResolveHolidayDate("劳动节", HolidayDateRelation.OnHolidayStart, new DateTime(2026, 4, 1))?.DueDate);
    }

    [Fact]
    public void ResolveDueDate_FallsBackToAnchorDateWhenProviderHasNoData()
    {
        using var context = new TemporaryHolidayCache();
        var service = new HolidayCalendarService(new NullHolidayScheduleProvider(), context.FilePath);

        Assert.Equal(new DateTime(2026, 5, 1), service.ResolveHolidayDate("劳动节", HolidayDateRelation.OnHolidayStart, new DateTime(2026, 4, 1))?.DueDate);
        Assert.False(service.ResolveHolidayDate("劳动节", HolidayDateRelation.OnHolidayStart, new DateTime(2026, 4, 1))?.UsesOfficialSchedule);
        Assert.Equal(new DateTime(2026, 4, 6), service.ResolveHolidayDate("清明节", HolidayDateRelation.AfterHoliday, new DateTime(2026, 4, 1))?.DueDate);
    }

    [Fact]
    public async Task ResolveDueDate_RollsToNextYearWhenCurrentYearHasPassed()
    {
        using var context = new TemporaryHolidayCache();
        var service = new HolidayCalendarService(new StubHolidayScheduleProvider([
            new HolidayYearSchedule(2026,
            [
                new HolidayRange("劳动节", new DateTime(2026, 5, 1), new DateTime(2026, 5, 5), HolidayCatalog.GetAliases("劳动节"))
            ]),
            new HolidayYearSchedule(2027,
            [
                new HolidayRange("劳动节", new DateTime(2027, 5, 1), new DateTime(2027, 5, 5), HolidayCatalog.GetAliases("劳动节"))
            ])
        ]), context.FilePath);

        await service.WarmupAsync(new DateTime(2026, 10, 1));

        Assert.Equal(new DateTime(2027, 5, 1), service.ResolveHolidayDate("劳动节", HolidayDateRelation.OnHolidayStart, new DateTime(2026, 10, 1))?.DueDate);
    }

    [Fact]
    public async Task WarmupAsync_ShowsNotPublishedStatusWhenFutureYearDataIsMissing()
    {
        using var context = new TemporaryHolidayCache();
        var service = new HolidayCalendarService(new NotPublishedHolidayScheduleProvider(), context.FilePath);

        await service.WarmupAsync(new DateTime(2026, 4, 1));

        Assert.Equal("节假日数据暂未发布（2026、2027），已回退节日锚点", service.LastWarmupStatusMessage);
    }

    private sealed class StubHolidayScheduleProvider(IEnumerable<HolidayYearSchedule> schedules) : IHolidayScheduleProvider
    {
        private readonly Dictionary<int, HolidayYearSchedule> _schedules = schedules.ToDictionary(item => item.Year);

        public Task<HolidayScheduleFetchResult> FetchYearAsync(int year, CancellationToken cancellationToken = default)
        {
            _schedules.TryGetValue(year, out var schedule);
            return Task.FromResult(
                schedule is null
                    ? new HolidayScheduleFetchResult(HolidayScheduleFetchStatus.NotPublished, null)
                    : new HolidayScheduleFetchResult(HolidayScheduleFetchStatus.Success, schedule));
        }
    }

    private sealed class NullHolidayScheduleProvider : IHolidayScheduleProvider
    {
        public Task<HolidayScheduleFetchResult> FetchYearAsync(int year, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HolidayScheduleFetchResult(HolidayScheduleFetchStatus.Failed, null));
        }
    }

    private sealed class NotPublishedHolidayScheduleProvider : IHolidayScheduleProvider
    {
        public Task<HolidayScheduleFetchResult> FetchYearAsync(int year, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HolidayScheduleFetchResult(HolidayScheduleFetchStatus.NotPublished, null));
        }
    }

    private sealed class TemporaryHolidayCache : IDisposable
    {
        public string FilePath { get; } = Path.Combine(Path.GetTempPath(), $"todoapp-holiday-cache-{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
    }
}
