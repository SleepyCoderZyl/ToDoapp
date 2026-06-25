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

    [Theory]
    [InlineData(10, 30, true)]
    [InlineData(0, 0, true)]
    [InlineData(23, 59, true)]
    public void DueTime_AcceptsValidValues(int hour, int minute, bool expected)
    {
        var todo = new TodoItem
        {
            Title = "时间校验",
            CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0)
        };

        todo.DueTime = new TimeOnly(hour, minute);

        Assert.Equal(expected, todo.DueTime.HasValue);
        Assert.Equal(new TimeOnly(hour, minute), todo.DueTime);
    }

    [Theory]
    [InlineData(24, 0)]
    [InlineData(25, 30)]
    [InlineData(10, 60)]
    [InlineData(10, 99)]
    public void DueTime_RejectsInvalidValues(int hour, int minute)
    {
        var todo = new TodoItem
        {
            Title = "时间校验",
            CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0)
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => todo.DueTime = new TimeOnly(hour, minute));
    }

    [Fact]
    public void ReminderOffsetMinutes_RejectsNegative()
    {
        var todo = new TodoItem
        {
            Title = "提前量校验",
            CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0)
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => todo.ReminderOffsetMinutes = -1);
    }

    [Fact]
    public void ReminderOffsetMinutes_RejectsAboveMaximum()
    {
        var todo = new TodoItem
        {
            Title = "提前量校验",
            CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0)
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => todo.ReminderOffsetMinutes = AppConstants.MaxReminderOffsetMinutes + 1);
    }

    [Fact]
    public void GetReminderTriggerTime_WithDateAndTime_ComputesOffsetCorrectly()
    {
        var todo = new TodoItem
        {
            Title = "提醒时间",
            CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0),
            DueDate = new DateTime(2026, 5, 13),
            DueTime = new TimeOnly(15, 0),
            ReminderOffsetMinutes = 10,
            HasReminder = true
        };

        var trigger = todo.GetReminderTriggerTime();

        Assert.Equal(new DateTime(2026, 5, 13, 14, 50, 0), trigger);
    }

    [Fact]
    public void GetReminderTriggerTime_WithoutDueTime_ReturnsNull()
    {
        var todo = new TodoItem
        {
            Title = "全天任务",
            CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0),
            DueDate = new DateTime(2026, 5, 13),
            DueTime = null,
            ReminderOffsetMinutes = 0,
            HasReminder = true
        };

        Assert.Null(todo.GetReminderTriggerTime());
    }

    [Fact]
    public void GetReminderTriggerTime_WithoutDueDate_ReturnsNull()
    {
        var todo = new TodoItem
        {
            Title = "无截止",
            CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0),
            HasReminder = true
        };

        Assert.Null(todo.GetReminderTriggerTime());
    }

    [Fact]
    public void GetReminderTriggerTime_WithoutHasReminder_ReturnsNull()
    {
        var todo = new TodoItem
        {
            Title = "未开启提醒",
            CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0),
            DueDate = new DateTime(2026, 5, 13),
            DueTime = new TimeOnly(15, 0),
            HasReminder = false
        };

        Assert.Null(todo.GetReminderTriggerTime());
    }

    [Theory]
    [InlineData(0, "准时")]
    [InlineData(5, "提前 5 分钟")]
    [InlineData(30, "提前 30 分钟")]
    [InlineData(60, "提前 1 小时")]
    [InlineData(120, "提前 2 小时")]
    public void ReminderOffsetDisplay_FormatsAccordingToMinutes(int offsetMinutes, string expectedDisplay)
    {
        var todo = new TodoItem
        {
            Title = "提前量展示",
            CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0),
            DueDate = new DateTime(2026, 5, 13),
            DueTime = new TimeOnly(15, 0),
            ReminderOffsetMinutes = offsetMinutes,
            HasReminder = true
        };

        Assert.Equal(expectedDisplay, todo.ReminderOffsetDisplay);
    }

    [Theory]
    [InlineData(9, 0, "09:00")]
    [InlineData(15, 30, "15:30")]
    [InlineData(0, 0, "00:00")]
    public void ReminderTimeDisplay_FormatsTime(int hour, int minute, string expected)
    {
        var todo = new TodoItem
        {
            Title = "时间展示",
            CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0),
            DueDate = new DateTime(2026, 5, 13),
            DueTime = new TimeOnly(hour, minute),
            HasReminder = true
        };

        Assert.Equal(expected, todo.ReminderTimeDisplay);
    }

    [Fact]
    public void ReminderTimeDisplay_WithoutTime_FallsBackToAllDay()
    {
        var todo = new TodoItem
        {
            Title = "全天",
            CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0),
            DueDate = new DateTime(2026, 5, 13),
            HasReminder = true
        };

        Assert.Equal("全天", todo.ReminderTimeDisplay);
    }

    [Fact]
    public void ReminderTimeDisplay_WithoutHasReminder_IsEmpty()
    {
        var todo = new TodoItem
        {
            Title = "无提醒",
            CreatedDate = new DateTime(2026, 5, 13, 9, 0, 0),
            DueDate = new DateTime(2026, 5, 13),
            DueTime = new TimeOnly(15, 0),
            HasReminder = false
        };

        Assert.Equal(string.Empty, todo.ReminderTimeDisplay);
        Assert.Equal(string.Empty, todo.ReminderOffsetDisplay);
    }
}
