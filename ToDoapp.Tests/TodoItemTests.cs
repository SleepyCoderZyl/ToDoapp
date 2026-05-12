using System.Collections.Generic;
using ToDoapp.Models;
using Xunit;

namespace ToDoapp.Tests;

public class TodoItemTests
{
    [Fact]
    public void RefreshTimeSensitiveProperties_RaisesNotificationsForDerivedTimeFields()
    {
        var todoItem = new TodoItem
        {
            Title = "跨天测试",
            CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0),
            DueDate = DateTime.Today.AddDays(1),
            IsDeleted = true
        };

        var changedProperties = new List<string>();
        todoItem.PropertyChanged += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.PropertyName))
            {
                changedProperties.Add(args.PropertyName!);
            }
        };

        todoItem.RefreshTimeSensitiveProperties();

        Assert.Contains(nameof(TodoItem.IsOverdue), changedProperties);
        Assert.Contains(nameof(TodoItem.DueDateDisplay), changedProperties);
        Assert.Contains(nameof(TodoItem.DaysUntilDue), changedProperties);
        Assert.Contains(nameof(TodoItem.DaysUntilPermanentDelete), changedProperties);
        Assert.Contains(nameof(TodoItem.DeleteTimeDisplay), changedProperties);
    }
}
