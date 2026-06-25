using ToDoapp.Models;
using ToDoapp.Services;
using Xunit;

namespace ToDoapp.Tests;

public class TodoReminderServiceTests
{
    [Fact]
    public void Scan_WhenTriggerTimeReached_IncludesTodo()
    {
        var service = new TodoReminderService();
        var todo = CreateTodo(
            title: "提交周报",
            dueDate: new DateTime(2026, 5, 13),
            dueTime: new TimeOnly(15, 0),
            offsetMinutes: 10);
        var now = new DateTime(2026, 5, 13, 14, 50, 0);

        var matches = service.Scan(new[] { todo }, now);

        var match = Assert.Single(matches);
        Assert.Same(todo, match.TodoItem);
        Assert.Equal(new DateTime(2026, 5, 13, 14, 50, 0), match.TriggerTime);
        Assert.Equal(ReminderKind.Todo, match.Snapshot.ReminderKind);
        Assert.Contains("提交周报", match.Snapshot.TitleText);
    }

    [Fact]
    public void Scan_WhenNowBeforeTrigger_ExcludesTodo()
    {
        var service = new TodoReminderService();
        var todo = CreateTodo(
            title: "提交周报",
            dueDate: new DateTime(2026, 5, 13),
            dueTime: new TimeOnly(15, 0),
            offsetMinutes: 10);
        var now = new DateTime(2026, 5, 13, 14, 30, 0);

        var matches = service.Scan(new[] { todo }, now);

        Assert.Empty(matches);
    }

    [Fact]
    public void Scan_WhenHasReminderDisabled_ExcludesTodo()
    {
        var service = new TodoReminderService();
        var todo = CreateTodo(
            title: "随便写写",
            dueDate: new DateTime(2026, 5, 13),
            dueTime: new TimeOnly(15, 0),
            offsetMinutes: 0,
            hasReminder: false);
        var now = new DateTime(2026, 5, 13, 16, 0, 0);

        var matches = service.Scan(new[] { todo }, now);

        Assert.Empty(matches);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Scan_SkipsCompletedTodos(bool isCompleted)
    {
        var service = new TodoReminderService();
        var todo = CreateTodo(
            title: "已办任务",
            dueDate: new DateTime(2026, 5, 13),
            dueTime: new TimeOnly(15, 0),
            offsetMinutes: 0);
        todo.IsCompleted = isCompleted;
        if (isCompleted)
        {
            todo.CompletedDate = new DateTime(2026, 5, 13, 14, 0, 0);
        }

        var now = new DateTime(2026, 5, 13, 16, 0, 0);

        var matches = service.Scan(new[] { todo }, now);

        if (isCompleted)
        {
            Assert.Empty(matches);
        }
        else
        {
            Assert.Single(matches);
        }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Scan_SkipsDeletedAndArchivedTodos(bool isDeleted, bool isArchived)
    {
        var service = new TodoReminderService();
        var todo = CreateTodo(
            title: "已处置任务",
            dueDate: new DateTime(2026, 5, 13),
            dueTime: new TimeOnly(15, 0),
            offsetMinutes: 0);
        if (isDeleted) todo.IsDeleted = true;
        if (isArchived) todo.IsArchived = true;

        var now = new DateTime(2026, 5, 13, 16, 0, 0);
        var matches = service.Scan(new[] { todo }, now);

        if (isDeleted || isArchived)
        {
            Assert.Empty(matches);
        }
        else
        {
            Assert.Single(matches);
        }
    }

    [Fact]
    public void Scan_DoesNotIncludeTwiceWithinSameTriggerWindow()
    {
        var service = new TodoReminderService();
        var todo = CreateTodo(
            title: "周会",
            dueDate: new DateTime(2026, 5, 13),
            dueTime: new TimeOnly(9, 0),
            offsetMinutes: 5);
        var trigger = new DateTime(2026, 5, 13, 8, 55, 0);

        var firstScan = service.Scan(new[] { todo }, trigger);
        Assert.Single(firstScan);

        TodoReminderService.MarkShown(firstScan[0].TodoItem, firstScan[0].TriggerTime);
        var secondScan = service.Scan(new[] { todo }, trigger.AddMinutes(3));
        Assert.Empty(secondScan);
    }

    [Fact]
    public void Scan_ReFiresAfterLastReminderShownAtIsResetBeforeTrigger()
    {
        var service = new TodoReminderService();
        var todo = CreateTodo(
            title: "周会",
            dueDate: new DateTime(2026, 5, 13),
            dueTime: new TimeOnly(9, 0),
            offsetMinutes: 5);
        var trigger = new DateTime(2026, 5, 13, 8, 55, 0);

        var firstScan = service.Scan(new[] { todo }, trigger);
        Assert.Single(firstScan);
        TodoReminderService.MarkShown(firstScan[0].TodoItem, firstScan[0].TriggerTime);

        // 模拟 LastReminderShownAt 早于 TriggerTime（例如应用清理或重置）
        todo.LastReminderShownAt = trigger.AddMinutes(-30);
        var secondScan = service.Scan(new[] { todo }, trigger.AddMinutes(10));
        Assert.Single(secondScan);
    }

    [Fact]
    public void Scan_ReFiresWhenLastReminderShownAtBeforeTrigger()
    {
        var service = new TodoReminderService();
        var todo = CreateTodo(
            title: "复盘",
            dueDate: new DateTime(2026, 5, 13),
            dueTime: new TimeOnly(9, 0),
            offsetMinutes: 0);
        todo.LastReminderShownAt = new DateTime(2026, 5, 12, 8, 0, 0);
        var now = new DateTime(2026, 5, 13, 9, 5, 0);

        var matches = service.Scan(new[] { todo }, now);

        Assert.Single(matches);
    }

    [Fact]
    public void Scan_NoDueDate_ExcludesTodo()
    {
        var service = new TodoReminderService();
        var todo = CreateTodo(
            title: "无截止任务",
            dueDate: null,
            dueTime: new TimeOnly(9, 0),
            offsetMinutes: 0);
        var now = new DateTime(2026, 5, 13, 9, 5, 0);

        var matches = service.Scan(new[] { todo }, now);

        Assert.Empty(matches);
    }

    [Fact]
    public void Scan_NullInput_Throws()
    {
        var service = new TodoReminderService();
        Assert.Throws<ArgumentNullException>(() => service.Scan(null!, DateTime.Now));
    }

    [Fact]
    public void MarkShown_UpdatesLastReminderShownAt()
    {
        var todo = CreateTodo(
            title: "待办",
            dueDate: new DateTime(2026, 5, 13),
            dueTime: new TimeOnly(9, 0),
            offsetMinutes: 0);
        var triggeredAt = new DateTime(2026, 5, 13, 8, 30, 0);

        TodoReminderService.MarkShown(todo, triggeredAt);

        Assert.Equal(triggeredAt, todo.LastReminderShownAt);
    }

    [Fact]
    public void MarkShown_NullTodo_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => TodoReminderService.MarkShown(null!, DateTime.Now));
    }

    [Fact]
    public void Scan_BuildsSnapshotContainingDueDateAndOffset()
    {
        var service = new TodoReminderService();
        var todo = CreateTodo(
            title: "复盘会",
            dueDate: new DateTime(2026, 5, 13),
            dueTime: new TimeOnly(15, 0),
            offsetMinutes: 15);
        var now = new DateTime(2026, 5, 13, 14, 45, 0);

        var matches = service.Scan(new[] { todo }, now);

        var match = Assert.Single(matches);
        Assert.Contains("2026-05-13", match.Snapshot.DescriptionText);
        Assert.Contains("15:00", match.Snapshot.DescriptionText);
        Assert.Contains("15 分钟", match.Snapshot.DescriptionText);
        Assert.Contains("复盘会", match.Snapshot.CustomReminders);
    }

    [Fact]
    public void Scan_FullDayWithoutTime_ExcludesTodo()
    {
        var service = new TodoReminderService();
        var todo = CreateTodo(
            title: "全天任务",
            dueDate: new DateTime(2026, 5, 13),
            dueTime: null,
            offsetMinutes: 0);
        var now = new DateTime(2026, 5, 13, 23, 59, 30);

        var matches = service.Scan(new[] { todo }, now);

        Assert.Empty(matches);
    }

    private static TodoItem CreateTodo(
        string title,
        DateTime? dueDate,
        TimeOnly? dueTime,
        int? offsetMinutes,
        bool hasReminder = true)
    {
        return new TodoItem
        {
            Title = title,
            CreatedDate = new DateTime(2026, 5, 1, 9, 0, 0),
            DueDate = dueDate,
            DueTime = dueTime,
            ReminderOffsetMinutes = offsetMinutes,
            HasReminder = hasReminder
        };
    }
}
