using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ToDoapp.Models;

namespace ToDoapp.Services;

public enum ReminderKind
{
    Startup,
    Scheduled
}

public class StartupReminderService
{
    private readonly TodoService _todoService;
    private readonly Func<AppSettings> _settingsAccessor;

    public StartupReminderService(TodoService? todoService = null, Func<AppSettings>? settingsAccessor = null)
    {
        _todoService = todoService ?? new TodoService();
        _settingsAccessor = settingsAccessor ?? (() => SettingsService.Instance.Settings);
    }

    public ReminderSnapshot CreateStartupSnapshot(DateTime now)
    {
        var loadResult = _todoService.LoadTodos();
        return BuildStartupSnapshot(loadResult.IsSuccess ? loadResult.Todos : [], _settingsAccessor(), now);
    }

    public ReminderSnapshot CreateScheduledSnapshot(DateTime now)
    {
        return BuildScheduledSnapshot(_settingsAccessor(), now);
    }

    public static ReminderSnapshot BuildStartupSnapshot(IEnumerable<TodoItem> todos, AppSettings settings, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(todos);
        ArgumentNullException.ThrowIfNull(settings);
        return BuildSnapshot(ReminderKind.Startup, settings.StartupReminderItems, now);
    }

    public static ReminderSnapshot BuildScheduledSnapshot(AppSettings settings, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return BuildSnapshot(ReminderKind.Scheduled, settings.ScheduledReminderItems, now);
    }

    public static bool ShouldShowScheduledReminder(AppSettings settings, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.ShowScheduledReminderDaily)
        {
            return false;
        }

        if (!TryParseScheduledReminderTime(settings.ScheduledReminderTime, out var scheduledTime))
        {
            return false;
        }

        if (!GetEnabledReminderTexts(settings.ScheduledReminderItems).Any())
        {
            return false;
        }

        var today = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (string.Equals(settings.LastScheduledReminderDate, today, StringComparison.Ordinal))
        {
            return false;
        }

        return now.Hour == scheduledTime.Hour && now.Minute == scheduledTime.Minute;
    }

    public static string GetScheduledReminderDateToken(DateTime now)
    {
        return now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static bool TryParseScheduledReminderTime(string? value, out TimeOnly scheduledTime)
    {
        return TimeOnly.TryParseExact(
            value,
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out scheduledTime);
    }

    private static ReminderSnapshot BuildSnapshot(ReminderKind reminderKind, IEnumerable<StartupReminderEntry>? items, DateTime now)
    {
        var customReminders = GetEnabledReminderTexts(items).ToList();
        return reminderKind switch
        {
            ReminderKind.Startup => new ReminderSnapshot(
                reminderKind,
                GetGreetingTitle(now),
                "展示你设置好的提醒内容。",
                "仅在开机自启时显示，可在设置中关闭。",
                customReminders),
            ReminderKind.Scheduled => new ReminderSnapshot(
                reminderKind,
                "这是你今天的定时提醒",
                "按你设置的时间，为你展示今天要记得的事。",
                "每天仅弹出一次，可在设置中调整时间或关闭。",
                customReminders),
            _ => throw new ArgumentOutOfRangeException(nameof(reminderKind), reminderKind, null)
        };
    }

    private static IEnumerable<string> GetEnabledReminderTexts(IEnumerable<StartupReminderEntry>? items)
    {
        return (items ?? [])
            .Where(item => item.IsEnabled && !string.IsNullOrWhiteSpace(item.Text))
            .Select(item => item.Text.Trim());
    }

    private static string GetGreetingTitle(DateTime now)
    {
        var greeting = now.Hour switch
        {
            >= 5 and < 12 => "早安",
            >= 12 and < 18 => "午安",
            _ => "晚上好"
        };

        return $"{greeting}，今天先看这几件事";
    }
}

public sealed record ReminderSnapshot(
    ReminderKind ReminderKind,
    string TitleText,
    string DescriptionText,
    string FooterHintText,
    IReadOnlyList<string> CustomReminders)
{
    public bool HasContent => CustomReminders.Count > 0;
    public bool HasCustomReminders => CustomReminders.Count > 0;
}
