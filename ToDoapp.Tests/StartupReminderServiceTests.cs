using ToDoapp.Models;
using ToDoapp.Services;
using Xunit;

namespace ToDoapp.Tests;

public class StartupReminderServiceTests
{
    [Fact]
    public void BuildStartupSnapshot_OnlyIncludesEnabledCustomReminders()
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

        var snapshot = StartupReminderService.BuildStartupSnapshot([], settings, new DateTime(2026, 4, 4, 9, 0, 0));

        Assert.Equal(["上班打卡", "写日报"], snapshot.CustomReminders);
        Assert.Equal(ReminderKind.Startup, snapshot.ReminderKind);
        Assert.Equal("早安，今天先看这几件事", snapshot.TitleText);
    }

    [Fact]
    public void BuildStartupSnapshot_ReturnsEmptySnapshotWhenNoContent()
    {
        var snapshot = StartupReminderService.BuildStartupSnapshot([], new AppSettings(), new DateTime(2026, 4, 4));

        Assert.False(snapshot.HasContent);
        Assert.Empty(snapshot.CustomReminders);
    }

    [Fact]
    public void BuildScheduledSnapshot_OnlyIncludesEnabledCustomReminders()
    {
        var settings = new AppSettings
        {
            ScheduledReminderItems =
            [
                new StartupReminderEntry { Text = "喝水", IsEnabled = true },
                new StartupReminderEntry { Text = "  站起来活动一下  ", IsEnabled = true },
                new StartupReminderEntry { Text = "摸鱼", IsEnabled = false }
            ]
        };

        var snapshot = StartupReminderService.BuildScheduledSnapshot(settings, new DateTime(2026, 4, 4, 18, 30, 0));

        Assert.Equal(ReminderKind.Scheduled, snapshot.ReminderKind);
        Assert.Equal(["喝水", "站起来活动一下"], snapshot.CustomReminders);
        Assert.Equal("这是你今天的定时提醒", snapshot.TitleText);
    }

    [Fact]
    public void ShouldShowScheduledReminder_ReturnsTrueOnlyAtConfiguredMinuteOncePerDay()
    {
        var settings = new AppSettings
        {
            ShowScheduledReminderDaily = true,
            ScheduledReminderTime = "09:00",
            ScheduledReminderItems =
            [
                new StartupReminderEntry { Text = "开会", IsEnabled = true }
            ]
        };

        Assert.True(StartupReminderService.ShouldShowScheduledReminder(settings, new DateTime(2026, 4, 4, 9, 0, 5)));

        settings.LastScheduledReminderDate = StartupReminderService.GetScheduledReminderDateToken(new DateTime(2026, 4, 4));
        Assert.False(StartupReminderService.ShouldShowScheduledReminder(settings, new DateTime(2026, 4, 4, 9, 0, 35)));

        settings.LastScheduledReminderDate = null;
        Assert.False(StartupReminderService.ShouldShowScheduledReminder(settings, new DateTime(2026, 4, 4, 9, 1, 0)));
    }

    [Fact]
    public void ShouldShowScheduledReminder_ReturnsFalseWhenDisabledOrNoContentOrInvalidTime()
    {
        var settings = new AppSettings
        {
            ShowScheduledReminderDaily = false,
            ScheduledReminderTime = "09:00",
            ScheduledReminderItems =
            [
                new StartupReminderEntry { Text = "开会", IsEnabled = true }
            ]
        };

        Assert.False(StartupReminderService.ShouldShowScheduledReminder(settings, new DateTime(2026, 4, 4, 9, 0, 0)));

        settings.ShowScheduledReminderDaily = true;
        settings.ScheduledReminderItems[0].IsEnabled = false;
        Assert.False(StartupReminderService.ShouldShowScheduledReminder(settings, new DateTime(2026, 4, 4, 9, 0, 0)));

        settings.ScheduledReminderItems[0].IsEnabled = true;
        settings.ScheduledReminderItems[0].Text = " ";
        Assert.False(StartupReminderService.ShouldShowScheduledReminder(settings, new DateTime(2026, 4, 4, 9, 0, 0)));

        settings.ScheduledReminderItems[0].Text = "开会";
        settings.ScheduledReminderTime = "invalid";
        Assert.False(StartupReminderService.ShouldShowScheduledReminder(settings, new DateTime(2026, 4, 4, 9, 0, 0)));
    }
}
