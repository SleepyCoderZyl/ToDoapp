using System.Text.Json;
using ToDoapp.Models;
using Xunit;

namespace ToDoapp.Tests;

public class AppSettingsSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void Deserialize_OldSettings_UsesStartupReminderDefaults()
    {
        const string json = """
            {
              "autoStart": true,
              "startInWidgetMode": false
            }
            """;

        var settings = JsonSerializer.Deserialize<AppSettings>(json, Options);

        Assert.NotNull(settings);
        Assert.True(settings!.ShowStartupReminderOnAutoStart);
        Assert.NotNull(settings.StartupReminderItems);
        Assert.Empty(settings.StartupReminderItems);
        Assert.False(settings.ShowScheduledReminderDaily);
        Assert.Equal("09:00", settings.ScheduledReminderTime);
        Assert.NotNull(settings.ScheduledReminderItems);
        Assert.Empty(settings.ScheduledReminderItems);
        Assert.Null(settings.LastScheduledReminderDate);
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesReminderSettings()
    {
        var original = new AppSettings
        {
            ShowStartupReminderOnAutoStart = false,
            StartupReminderItems =
            [
                new StartupReminderEntry { Text = "上班打卡", IsEnabled = true },
                new StartupReminderEntry { Text = "写日报", IsEnabled = false }
            ],
            ShowScheduledReminderDaily = true,
            ScheduledReminderTime = "18:30",
            ScheduledReminderItems =
            [
                new StartupReminderEntry { Text = "下班收尾", IsEnabled = true },
                new StartupReminderEntry { Text = "明日计划", IsEnabled = false }
            ],
            LastScheduledReminderDate = "2026-05-13"
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<AppSettings>(json, Options);

        Assert.NotNull(restored);
        Assert.False(restored!.ShowStartupReminderOnAutoStart);
        Assert.Equal(2, restored.StartupReminderItems.Count);
        Assert.Equal("上班打卡", restored.StartupReminderItems[0].Text);
        Assert.True(restored.StartupReminderItems[0].IsEnabled);
        Assert.Equal("写日报", restored.StartupReminderItems[1].Text);
        Assert.False(restored.StartupReminderItems[1].IsEnabled);
        Assert.True(restored.ShowScheduledReminderDaily);
        Assert.Equal("18:30", restored.ScheduledReminderTime);
        Assert.Equal(2, restored.ScheduledReminderItems.Count);
        Assert.Equal("下班收尾", restored.ScheduledReminderItems[0].Text);
        Assert.True(restored.ScheduledReminderItems[0].IsEnabled);
        Assert.Equal("明日计划", restored.ScheduledReminderItems[1].Text);
        Assert.False(restored.ScheduledReminderItems[1].IsEnabled);
        Assert.Equal("2026-05-13", restored.LastScheduledReminderDate);
    }
}
