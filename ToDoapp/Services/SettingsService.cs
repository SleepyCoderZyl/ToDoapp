using System;
using System.IO;
using System.Text.Json;
using ToDoapp.Models;

namespace ToDoapp.Services;

public class SettingsService
{
    private static SettingsService? _instance;
    public static SettingsService Instance => _instance ??= new SettingsService();

    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private AppSettings _settings;

    public AppSettings Settings => _settings;

    public event EventHandler? SettingsChanged;

    private SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "ToDoApp");
        Directory.CreateDirectory(appFolder);
        _settingsFilePath = Path.Combine(appFolder, "settings.json");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        _settings = LoadSettings();
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
                if (settings != null)
                {
                    settings.Normalize();
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载设置失败: {ex.Message}");
        }
        
        var defaultSettings = new AppSettings();
        defaultSettings.Normalize();
        return defaultSettings;
    }

    public void SaveSettings()
    {
        PersistSettings(notifyChanged: true);
    }

    public void UpdateWidgetOpacity(double opacity)
    {
        _settings.WidgetOpacity = opacity;
        PersistSettings();
    }

    public void UpdateWidgetContentOpacity(double opacity)
    {
        _settings.WidgetContentOpacity = opacity;
        PersistSettings();
    }

    public void UpdateWindowPosition(double width, double height, double left, double top)
    {
        _settings.WindowWidth = width;
        _settings.WindowHeight = height;
        _settings.WindowLeft = left;
        _settings.WindowTop = top;
        SaveSettings();
    }

    public void UpdateWidgetModePosition(double width, double height, double left, double top)
    {
        _settings.WidgetModeWidth = width;
        _settings.WidgetModeHeight = height;
        _settings.WidgetModeLeft = left;
        _settings.WidgetModeTop = top;

        PersistSettings();
    }

    private void PersistSettings(bool notifyChanged = false)
    {
        try
        {
            _settings.LastUpdated = DateTime.Now;
            var json = JsonSerializer.Serialize(_settings, _jsonOptions);
            File.WriteAllText(_settingsFilePath, json);

            if (notifyChanged)
            {
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
        }
    }

    public void UpdateWidgetAlwaysOnTop(bool alwaysOnTop)
    {
        _settings.WidgetAlwaysOnTop = alwaysOnTop;
        SaveSettings();
    }

}
