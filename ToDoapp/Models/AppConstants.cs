namespace ToDoapp.Models;

public static class AppConstants
{
    public const int MaxTitleLength = 200;
    public const string UpdateCheckUrl = "https://api.github.com/repos/SleepyCoderZyl/ToDoapp/releases/latest";
    public const string UpdateDownloadUrl = "https://github.com/SleepyCoderZyl/ToDoapp/releases/latest";
    public const string UpdateUserAgent = "ToDoapp-UpdateChecker/1.0";

    public const int MaxReminderOffsetMinutes = 7 * 24 * 60;
    public const int DefaultReminderOffsetMinutes = 0;
}
