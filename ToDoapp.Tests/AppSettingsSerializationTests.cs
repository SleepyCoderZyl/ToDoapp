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
    }

    [Fact]
    public void SerializeAndDeserialize_PreservesStartupReminderSettings()
    {
        var original = new AppSettings
        {
            ShowStartupReminderOnAutoStart = false,
            StartupReminderItems =
            [
                new StartupReminderEntry { Text = "上班打卡", IsEnabled = true },
                new StartupReminderEntry { Text = "写日报", IsEnabled = false }
            ]
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
    }
}
