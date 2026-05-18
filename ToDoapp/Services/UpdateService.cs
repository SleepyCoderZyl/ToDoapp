using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using ToDoapp.Constants;

namespace ToDoapp.Services;

public sealed class UpdateService
{
    private readonly HttpClient _httpClient;
    private readonly string _updateCheckUrl;
    private readonly string _downloadUrl;

    public UpdateService(HttpClient? httpClient = null, string? updateCheckUrl = null, string? downloadUrl = null)
    {
        _httpClient = httpClient ?? CreateHttpClient();
        _updateCheckUrl = updateCheckUrl ?? AppConstants.UpdateCheckUrl;
        _downloadUrl = downloadUrl ?? AppConstants.UpdateDownloadUrl;
    }

    public string GetCurrentVersion()
    {
        return NormalizeVersionLabel(Assembly.GetExecutingAssembly().GetName().Version?.ToString()) ?? "1.4.0";
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = GetCurrentVersion();

        try
        {
            using var response = await _httpClient.GetAsync(_updateCheckUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return UpdateCheckResult.Failed(currentVersion, "检查更新失败", $"版本服务返回状态码 {(int)response.StatusCode}。请稍后重试。");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!TryParseRelease(document.RootElement, out var latestVersion, out var downloadUrl, out var releaseName, out var publishedAtText))
            {
                return UpdateCheckResult.Failed(currentVersion, "检查更新失败", "未能解析最新版本信息。请确认发布数据格式是否正确。");
            }

            if (!TryParseVersion(latestVersion, out var remoteVersion) || !TryParseVersion(currentVersion, out var localVersion))
            {
                return UpdateCheckResult.Failed(currentVersion, "检查更新失败", "版本号格式无法比较，请检查当前版本或发布版本格式。");
            }

            if (remoteVersion > localVersion)
            {
                var detailText = string.IsNullOrWhiteSpace(releaseName)
                    ? $"发现新版本 {latestVersion}。"
                    : $"发现新版本 {latestVersion}（{releaseName}）。";

                if (!string.IsNullOrWhiteSpace(publishedAtText))
                {
                    detailText += $" 发布时间：{publishedAtText}。";
                }

                return new UpdateCheckResult(
                    true,
                    true,
                    currentVersion,
                    latestVersion,
                    "发现新版本",
                    detailText,
                    string.IsNullOrWhiteSpace(downloadUrl) ? _downloadUrl : downloadUrl,
                    publishedAtText);
            }

            return new UpdateCheckResult(
                true,
                false,
                currentVersion,
                latestVersion,
                "当前已是最新版本",
                $"当前版本 {currentVersion} 已是最新版本。",
                string.IsNullOrWhiteSpace(downloadUrl) ? _downloadUrl : downloadUrl,
                publishedAtText);
        }
        catch (TaskCanceledException)
        {
            return UpdateCheckResult.Failed(currentVersion, "检查更新失败", "检查更新超时，请稍后重试。");
        }
        catch (HttpRequestException)
        {
            return UpdateCheckResult.Failed(currentVersion, "检查更新失败", "网络请求失败，请检查网络连接后重试。");
        }
        catch (JsonException)
        {
            return UpdateCheckResult.Failed(currentVersion, "检查更新失败", "版本信息解析失败，请稍后重试。");
        }
        catch (Exception ex)
        {
            return UpdateCheckResult.Failed(currentVersion, "检查更新失败", $"发生未预期错误：{ex.Message}");
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(AppConstants.UpdateUserAgent);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    private static bool TryParseRelease(
        JsonElement root,
        out string latestVersion,
        out string downloadUrl,
        out string releaseName,
        out string publishedAtText)
    {
        latestVersion = root.TryGetProperty("tag_name", out var tagElement)
            ? NormalizeVersionLabel(tagElement.GetString()) ?? string.Empty
            : string.Empty;
        downloadUrl = root.TryGetProperty("html_url", out var urlElement)
            ? urlElement.GetString() ?? string.Empty
            : string.Empty;
        releaseName = root.TryGetProperty("name", out var nameElement)
            ? nameElement.GetString() ?? string.Empty
            : string.Empty;
        publishedAtText = root.TryGetProperty("published_at", out var publishedAtElement)
            ? FormatPublishedAt(publishedAtElement.GetString())
            : string.Empty;

        return !string.IsNullOrWhiteSpace(latestVersion);
    }

    private static string? NormalizeVersionLabel(string? rawVersion)
    {
        if (string.IsNullOrWhiteSpace(rawVersion))
        {
            return null;
        }

        var normalized = rawVersion.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
        {
            normalized = normalized[1..];
        }

        return normalized;
    }

    private static bool TryParseVersion(string rawVersion, out Version version)
    {
        version = new Version();
        var normalized = NormalizeVersionLabel(rawVersion);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return Version.TryParse(normalized, out version);
    }

    private static string FormatPublishedAt(string? publishedAt)
    {
        if (string.IsNullOrWhiteSpace(publishedAt))
        {
            return string.Empty;
        }

        return DateTimeOffset.TryParse(publishedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            ? date.LocalDateTime.ToString("yyyy-MM-dd HH:mm")
            : publishedAt;
    }
}
