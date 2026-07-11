using System.Text.Json;
using ToDoapp.Services;
using Xunit;

namespace ToDoapp.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), $"ToDoapp-SettingsTests-{Guid.NewGuid():N}");

    [Fact]
    public void SaveSettings_ReplacesPrimaryAndKeepsPreviousVersionAsBackup()
    {
        var settingsPath = Path.Combine(_testDirectory, "settings.json");
        var service = new SettingsService(settingsPath);
        service.Settings.WidgetOpacity = 0.75;
        Assert.True(service.SaveSettings());

        service.Settings.WidgetOpacity = 0.5;
        Assert.True(service.SaveSettings());

        Assert.True(File.Exists(settingsPath));
        Assert.True(File.Exists($"{settingsPath}.bak"));
        Assert.False(File.Exists($"{settingsPath}.tmp"));

        using var primary = JsonDocument.Parse(File.ReadAllText(settingsPath));
        using var backup = JsonDocument.Parse(File.ReadAllText($"{settingsPath}.bak"));
        Assert.Equal(0.5, primary.RootElement.GetProperty("widgetOpacity").GetDouble());
        Assert.Equal(0.75, backup.RootElement.GetProperty("widgetOpacity").GetDouble());
    }

    [Fact]
    public void LoadSettings_WhenPrimaryIsCorrupted_RecoversFromBackup()
    {
        var settingsPath = Path.Combine(_testDirectory, "settings.json");
        var service = new SettingsService(settingsPath);
        service.Settings.WidgetOpacity = 0.65;
        Assert.True(service.SaveSettings());
        service.Settings.WidgetOpacity = 0.4;
        Assert.True(service.SaveSettings());
        File.WriteAllText(settingsPath, "{ invalid json");

        var recoveredService = new SettingsService(settingsPath);

        Assert.Equal(0.65, recoveredService.Settings.WidgetOpacity);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
