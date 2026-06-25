using System;
using System.Collections.Generic;
using System.Linq;
using ToDoapp.Models;

namespace ToDoapp.Services;

/// <summary>
/// 扫描待办并找出当前应弹窗的提醒。负责纯判定与文案生成，不触碰 UI 与持久化。
/// </summary>
public class TodoReminderService
{
    public IReadOnlyList<TodoReminderMatch> Scan(IEnumerable<TodoItem> todos, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(todos);
        var matches = new List<TodoReminderMatch>();
        foreach (var todoItem in todos)
        {
            if (todoItem is null)
            {
                continue;
            }

            if (todoItem.IsCompleted || todoItem.IsDeleted || todoItem.IsArchived)
            {
                continue;
            }

            if (!todoItem.HasReminder)
            {
                continue;
            }

            var trigger = todoItem.GetReminderTriggerTime();
            if (!trigger.HasValue)
            {
                continue;
            }

            if (now < trigger.Value)
            {
                continue;
            }

            if (IsAlreadyShownWithinWindow(todoItem, trigger.Value))
            {
                continue;
            }

            matches.Add(new TodoReminderMatch(todoItem, StartupReminderService.BuildTodoSnapshot(todoItem, now), trigger.Value));
        }

        return matches;
    }

    public static void MarkShown(TodoItem todoItem, DateTime triggeredAt)
    {
        ArgumentNullException.ThrowIfNull(todoItem);
        todoItem.LastReminderShownAt = triggeredAt;
    }

    private static bool IsAlreadyShownWithinWindow(TodoItem todoItem, DateTime triggerTime)
    {
        if (!todoItem.LastReminderShownAt.HasValue)
        {
            return false;
        }

        var lastShown = todoItem.LastReminderShownAt.Value;
        if (lastShown < triggerTime)
        {
            return false;
        }

        // 同一触发窗口（5 分钟）内不再弹
        return (lastShown - triggerTime) <= TimeSpan.FromMinutes(5);
    }
}

public sealed record TodoReminderMatch(TodoItem TodoItem, ReminderSnapshot Snapshot, DateTime TriggerTime);
