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
            ShowScheduledReminderDaily = true,
            ScheduledReminderItems =
            [
                new StartupReminderEntry { Text = "喝水", IsEnabled = true, ScheduledTime = "18:30" },
                new StartupReminderEntry { Text = "  站起来活动一下  ", IsEnabled = true, ScheduledTime = "18:30" },
                new StartupReminderEntry { Text = "摸鱼", IsEnabled = false, ScheduledTime = "18:30" },
                new StartupReminderEntry { Text = "明日计划", IsEnabled = true, ScheduledTime = "20:00" }
            ]
        };

        var snapshot = StartupReminderService.BuildScheduledSnapshot(settings, new DateTime(2026, 4, 4, 18, 30, 0));

        Assert.Equal(ReminderKind.Scheduled, snapshot.ReminderKind);
        Assert.Equal(["喝水", "站起来活动一下"], snapshot.CustomReminders);
        Assert.Equal("这是你今天的定时提醒", snapshot.TitleText);
    }

    [Fact]
    public void ShouldShowScheduledReminder_ReturnsTrueAtOrAfterDueTimeOncePerDay()
    {
        var settings = new AppSettings
        {
            ShowScheduledReminderDaily = true,
            ScheduledReminderItems =
            [
                new StartupReminderEntry { Text = "开会", IsEnabled = true, ScheduledTime = "09:00" }
            ]
        };

        Assert.True(StartupReminderService.ShouldShowScheduledReminder(settings, new DateTime(2026, 4, 4, 9, 0, 5)));
        Assert.True(StartupReminderService.ShouldShowScheduledReminder(settings, new DateTime(2026, 4, 4, 9, 1, 0)));

        settings.ScheduledReminderItems[0].LastScheduledReminderDate = StartupReminderService.GetScheduledReminderDateToken(new DateTime(2026, 4, 4));
        Assert.False(StartupReminderService.ShouldShowScheduledReminder(settings, new DateTime(2026, 4, 4, 9, 0, 35)));

        settings.ScheduledReminderItems[0].LastScheduledReminderDate = null;
        Assert.False(StartupReminderService.ShouldShowScheduledReminder(settings, new DateTime(2026, 4, 4, 8, 59, 59)));
    }

    [Fact]
    public void ShouldShowScheduledReminder_ReturnsFalseWhenDisabledOrNoContentOrInvalidTime()
    {
        var settings = new AppSettings
        {
            ShowScheduledReminderDaily = false,
            ScheduledReminderItems =
            [
                new StartupReminderEntry { Text = "开会", IsEnabled = true, ScheduledTime = "09:00" }
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
        settings.ScheduledReminderItems[0].ScheduledTime = "invalid";
        Assert.False(StartupReminderService.ShouldShowScheduledReminder(settings, new DateTime(2026, 4, 4, 9, 0, 0)));
    }

    [Fact]
    public void GetDueScheduledReminderEntries_AllowsDifferentReminderTimesOnSameDay()
    {
        var settings = new AppSettings
        {
            ShowScheduledReminderDaily = true,
            ScheduledReminderItems =
            [
                new StartupReminderEntry { Text = "早会", IsEnabled = true, ScheduledTime = "09:00" },
                new StartupReminderEntry { Text = "日报", IsEnabled = true, ScheduledTime = "18:30" }
            ]
        };

        var morningEntries = StartupReminderService.GetDueScheduledReminderEntries(settings, new DateTime(2026, 4, 4, 9, 0, 0));
        Assert.Single(morningEntries);
        Assert.Equal("早会", morningEntries[0].Text);

        StartupReminderService.MarkScheduledRemindersShown(morningEntries, new DateTime(2026, 4, 4, 9, 0, 0));

        var eveningEntries = StartupReminderService.GetDueScheduledReminderEntries(settings, new DateTime(2026, 4, 4, 18, 30, 0));
        Assert.Single(eveningEntries);
        Assert.Equal("日报", eveningEntries[0].Text);
    }

    [Fact]
    public void BuildScheduledSnapshot_MergesRemindersAtSameMinute()
    {
        var settings = new AppSettings
        {
            ShowScheduledReminderDaily = true,
            ScheduledReminderItems =
            [
                new StartupReminderEntry { Text = "喝水", IsEnabled = true, ScheduledTime = "10:15" },
                new StartupReminderEntry { Text = "活动一下", IsEnabled = true, ScheduledTime = "10:15" }
            ]
        };

        var snapshot = StartupReminderService.BuildScheduledSnapshot(settings, new DateTime(2026, 4, 4, 10, 15, 30));

        Assert.Equal(["喝水", "活动一下"], snapshot.CustomReminders);
    }
}
