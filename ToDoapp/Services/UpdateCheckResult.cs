namespace ToDoapp.Services;

public sealed record UpdateCheckResult(
    bool IsSuccess,
    bool HasUpdate,
    string CurrentVersion,
    string LatestVersion,
    string StatusText,
    string DetailText,
    string? DownloadUrl,
    string? PublishedAtText)
{
    public static UpdateCheckResult Failed(string currentVersion, string statusText, string detailText)
    {
        return new UpdateCheckResult(
            false,
            false,
            currentVersion,
            currentVersion,
            statusText,
            detailText,
            null,
            null);
    }
}
