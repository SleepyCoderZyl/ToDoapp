using ToDoapp.Views;
using Xunit;

namespace ToDoapp.Tests;

public class MainWindowReminderTimingTests
{
    [Fact]
    public void CalculateEarliestReminderTriggerTime_OnStartup_AllowsOneMinuteCatchUp()
    {
        var now = new DateTime(2026, 5, 13, 9, 0, 0);

        var earliest = MainWindow.CalculateEarliestReminderTriggerTime(now, default, default);

        Assert.Equal(now.AddMinutes(-1), earliest);
    }

    [Fact]
    public void CalculateEarliestReminderTriggerTime_AfterShortPause_PreservesExistingCutoff()
    {
        var lastCheck = new DateTime(2026, 5, 13, 9, 0, 0);
        var existingCutoff = lastCheck.AddMinutes(-1);

        var earliest = MainWindow.CalculateEarliestReminderTriggerTime(
            lastCheck.AddSeconds(45),
            lastCheck,
            existingCutoff);

        Assert.Equal(existingCutoff, earliest);
    }

    [Fact]
    public void CalculateEarliestReminderTriggerTime_AfterLongPause_AdvancesCutoff()
    {
        var lastCheck = new DateTime(2026, 5, 13, 9, 0, 0);
        var now = lastCheck.AddMinutes(10);

        var earliest = MainWindow.CalculateEarliestReminderTriggerTime(
            now,
            lastCheck,
            lastCheck.AddMinutes(-1));

        Assert.Equal(now.AddMinutes(-1), earliest);
    }

    [Fact]
    public void CalculateEarliestReminderTriggerTime_WhenClockMovesBackward_ResetsCutoff()
    {
        var lastCheck = new DateTime(2026, 5, 13, 9, 0, 0);
        var now = lastCheck.AddMinutes(-30);

        var earliest = MainWindow.CalculateEarliestReminderTriggerTime(
            now,
            lastCheck,
            lastCheck.AddMinutes(-1));

        Assert.Equal(now.AddMinutes(-1), earliest);
    }
}
