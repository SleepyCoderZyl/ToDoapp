using System;

namespace ToDoapp.Models;

internal sealed class TodoStorageItem
{
    public string Title { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public DateTime? DueDate { get; set; }

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
            HasReminder = todoItem.HasReminder,
            IsDeleted = todoItem.IsDeleted,
            DeletedDate = todoItem.DeletedDate,
            IsArchived = todoItem.IsArchived,
            ArchivedDate = todoItem.ArchivedDate
        };
    }
}
