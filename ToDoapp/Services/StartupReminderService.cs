using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ToDoapp.Models;

namespace ToDoapp.Services;

public enum ReminderKind
{
    Startup,
    Scheduled,
    Todo
}

public class StartupReminderService
{
    private readonly ITodoService _todoService;
    private readonly Func<AppSettings> _settingsAccessor;

    public StartupReminderService(ITodoService? todoService = null, Func<AppSettings>? settingsAccessor = null)
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

    public static ReminderSnapshot BuildTodoSnapshot(TodoItem todoItem, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(todoItem);
        var dueText = todoItem.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "未指定";
        var timeText = todoItem.DueTime?.ToString("HH:mm", CultureInfo.InvariantCulture);
        var offsetText = todoItem.ReminderOffsetMinutes.HasValue && todoItem.ReminderOffsetMinutes.Value > 0
            ? $"提前 {todoItem.ReminderOffsetMinutes.Value} 分钟"
            : "准时";

        var lines = new List<string>
        {
            $"截止：{dueText}{(string.IsNullOrEmpty(timeText) ? string.Empty : " " + timeText)}（{offsetText}）"
        };
        if (todoItem.GetReminderTriggerTime() is { } trigger)
        {
            lines.Add($"触发时间：{trigger.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}（当前 {now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}）");
        }

        return new ReminderSnapshot(
            ReminderKind.Todo,
            $"待办提醒：{todoItem.Title}",
            string.Join("  ·  ", lines),
            "可以点击\"知道了\"关闭，或点击\"打开主窗口\"查看全部。",
            [todoItem.Title]);
    }

    public static ReminderSnapshot BuildStartupSnapshot(IEnumerable<TodoItem> todos, AppSettings settings, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(todos);
        ArgumentNullException.ThrowIfNull(settings);
        return BuildSnapshot(ReminderKind.Startup, GetEnabledReminderTexts(settings.StartupReminderItems), now);
    }

    public static ReminderSnapshot BuildScheduledSnapshot(AppSettings settings, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var dueReminderTexts = GetDueScheduledReminderEntries(settings, now)
            .Select(item => item.Text.Trim());
        return BuildSnapshot(ReminderKind.Scheduled, dueReminderTexts, now);
    }

    public static bool ShouldShowScheduledReminder(AppSettings settings, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return GetDueScheduledReminderEntries(settings, now).Any();
    }

    public static IReadOnlyList<StartupReminderEntry> GetDueScheduledReminderEntries(AppSettings settings, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.ShowScheduledReminderDaily)
        {
            return [];
        }

        var today = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return (settings.ScheduledReminderItems ?? [])
            .Where(item => IsScheduledReminderDue(item, settings, now, today))
            .ToList();
    }

    public static string GetScheduledReminderDateToken(DateTime now)
    {
        return now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public static void MarkScheduledRemindersShown(IEnumerable<StartupReminderEntry> reminderEntries, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(reminderEntries);
        var today = GetScheduledReminderDateToken(now);
        foreach (var reminderEntry in reminderEntries)
        {
            reminderEntry.LastScheduledReminderDate = today;
        }
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

    private static ReminderSnapshot BuildSnapshot(ReminderKind reminderKind, IEnumerable<string> reminderTexts, DateTime now)
    {
        var customReminders = reminderTexts
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => text.Trim())
            .ToList();
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

    private static bool IsScheduledReminderDue(StartupReminderEntry item, AppSettings settings, DateTime now, string today)
    {
        if (!item.IsEnabled || string.IsNullOrWhiteSpace(item.Text))
        {
            return false;
        }

        var scheduledTimeText = string.IsNullOrWhiteSpace(item.ScheduledTime)
            ? settings.ScheduledReminderTime
            : item.ScheduledTime;
        if (!TryParseScheduledReminderTime(scheduledTimeText, out var scheduledTime))
        {
            return false;
        }

        if (string.Equals(item.LastScheduledReminderDate, today, StringComparison.Ordinal))
        {
            return false;
        }

        return TimeOnly.FromDateTime(now) >= scheduledTime;
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
