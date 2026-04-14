using System;
using System.Collections.Generic;
using System.Linq;
using ToDoapp.Models;

namespace ToDoapp.Services;

public class StartupReminderService
{
    private readonly TodoService _todoService;
    private readonly Func<AppSettings> _settingsAccessor;

    public StartupReminderService(TodoService? todoService = null, Func<AppSettings>? settingsAccessor = null)
    {
        _todoService = todoService ?? new TodoService();
        _settingsAccessor = settingsAccessor ?? (() => SettingsService.Instance.Settings);
    }

    public StartupReminderSnapshot CreateSnapshot(DateTime now)
    {
        return BuildSnapshot(_todoService.LoadTodos(), _settingsAccessor(), now);
    }

    public static StartupReminderSnapshot BuildSnapshot(IEnumerable<TodoItem> todos, AppSettings settings, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(todos);
        ArgumentNullException.ThrowIfNull(settings);
        _ = now;
        settings.StartupReminderItems ??= [];

        var customReminders = settings.StartupReminderItems
            .Where(item => item.IsEnabled && !string.IsNullOrWhiteSpace(item.Text))
            .Select(item => item.Text.Trim())
            .ToList();

        return new StartupReminderSnapshot(customReminders);
    }
}

public sealed record StartupReminderSnapshot(
    IReadOnlyList<string> CustomReminders)
{
    public bool HasContent => CustomReminders.Count > 0;
    public bool HasCustomReminders => CustomReminders.Count > 0;
}
