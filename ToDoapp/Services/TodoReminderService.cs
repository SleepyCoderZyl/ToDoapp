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
    /// <summary>
    /// 查找触发时间位于指定下限与当前时间之间、且尚未显示的待办提醒。
    /// </summary>
    /// <param name="todos">要扫描的待办集合。</param>
    /// <param name="now">本次扫描使用的当前本地时间。</param>
    /// <param name="earliestTriggerTime">本次允许补弹的最早触发时间，包含边界。</param>
    /// <returns>当前应显示的待办提醒。</returns>
    public IReadOnlyList<TodoReminderMatch> Scan(
        IEnumerable<TodoItem> todos,
        DateTime now,
        DateTime earliestTriggerTime)
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

            if (trigger.Value < earliestTriggerTime || now < trigger.Value)
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
