using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace ToDoapp.Models;

internal sealed class TodoStorageItem
{
    public string Title { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public DateTime? DueDate { get; set; }

    [JsonPropertyName("dueTime")]
    public TimeOnly? DueTime { get; set; }

    [JsonPropertyName("reminderOffsetMinutes")]
    public int? ReminderOffsetMinutes { get; set; }

    [JsonPropertyName("lastReminderShownAt")]
    public DateTime? LastReminderShownAt { get; set; }

    public bool HasReminder { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedDate { get; set; }

    public bool IsArchived { get; set; }

    public DateTime? ArchivedDate { get; set; }

    public static TodoStorageItem FromTodoItem(TodoItem todoItem)
    {
        ArgumentNullException.ThrowIfNull(todoItem);

        return new TodoStorageItem
        {
            Title = todoItem.Title,
            IsCompleted = todoItem.IsCompleted,
            CreatedDate = todoItem.CreatedDate,
            CompletedDate = todoItem.CompletedDate,
            DueDate = todoItem.DueDate,
            DueTime = todoItem.DueTime,
            ReminderOffsetMinutes = todoItem.ReminderOffsetMinutes,
            LastReminderShownAt = todoItem.LastReminderShownAt,
            HasReminder = todoItem.HasReminder,
            IsDeleted = todoItem.IsDeleted,
            DeletedDate = todoItem.DeletedDate,
            IsArchived = todoItem.IsArchived,
            ArchivedDate = todoItem.ArchivedDate
        };
    }
}
