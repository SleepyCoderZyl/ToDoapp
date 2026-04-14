using ToDoapp.Models;
using ToDoapp.Services;
using Xunit;

namespace ToDoapp.Tests;

public class StartupReminderServiceTests
{
    [Fact]
    public void BuildSnapshot_OnlyIncludesEnabledCustomReminders()
    {
        var settings = new AppSettings
        {
            StartupReminderItems =
            [
                new StartupReminderEntry { Text = "上班打卡", IsEnabled = true },
                new StartupReminderEntry { Text = "  写日报  ", IsEnabled = true },
                new StartupReminderEntry { Text = "喝水", IsEnabled = false },
                new StartupReminderEntry { Text = "   ", IsEnabled = true }
            ]
        };

        var snapshot = StartupReminderService.BuildSnapshot([], settings, new DateTime(2026, 4, 4));

        Assert.Equal(["上班打卡", "写日报"], snapshot.CustomReminders);
    }

    [Fact]
    public void BuildSnapshot_ReturnsEmptySnapshotWhenNoContent()
    {
        var snapshot = StartupReminderService.BuildSnapshot([], new AppSettings(), new DateTime(2026, 4, 4));

        Assert.False(snapshot.HasContent);
        Assert.Empty(snapshot.CustomReminders);
    }
}
