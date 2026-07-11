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
    private readonly string _settingsBackupFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private AppSettings _settings;

    public AppSettings Settings => _settings;

    public event EventHandler? SettingsChanged;
    public event EventHandler<SettingsSaveFailedEventArgs>? SettingsSaveFailed;

    private SettingsService()
        : this(GetDefaultSettingsFilePath())
    {
    }

    internal SettingsService(string settingsFilePath)
    {
        _settingsFilePath = Path.GetFullPath(settingsFilePath);
        _settingsBackupFilePath = $"{_settingsFilePath}.bak";
        var directoryPath = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        _settings = LoadSettings();
    }

    private AppSettings LoadSettings()
    {
        if (TryLoadSettings(_settingsFilePath, out var settings) ||
            TryLoadSettings(_settingsBackupFilePath, out settings))
        {
            return settings;
        }

        var defaultSettings = new AppSettings();
        defaultSettings.Normalize();
        return defaultSettings;
    }

    private bool TryLoadSettings(string filePath, out AppSettings settings)
    {
        settings = null!;
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
            if (loadedSettings == null)
            {
                return false;
            }

            loadedSettings.Normalize();
            settings = loadedSettings;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载设置失败 ({filePath}): {ex.Message}");
            return false;
        }
    }

    public bool SaveSettings()
    {
        return PersistSettings(notifyChanged: true);
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

    private bool PersistSettings(bool notifyChanged = false)
    {
        var tempFilePath = $"{_settingsFilePath}.tmp";
        try
        {
            _settings.LastUpdated = DateTime.Now;
            var json = JsonSerializer.Serialize(_settings, _jsonOptions);
            File.WriteAllText(tempFilePath, json);

            if (!TryLoadSettings(tempFilePath, out _))
            {
                throw new InvalidDataException("设置文件写入校验失败。");
            }

            if (File.Exists(_settingsFilePath))
            {
                File.Replace(tempFilePath, _settingsFilePath, _settingsBackupFilePath, true);
            }
            else
            {
                File.Move(tempFilePath, _settingsFilePath);
            }

            if (notifyChanged)
            {
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存设置失败: {ex.Message}");
            SettingsSaveFailed?.Invoke(this, new SettingsSaveFailedEventArgs(ex.Message));
            return false;
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"清理设置临时文件失败: {ex.Message}");
                }
            }
        }
    }

    public void UpdateWidgetAlwaysOnTop(bool alwaysOnTop)
    {
        _settings.WidgetAlwaysOnTop = alwaysOnTop;
        SaveSettings();
    }

    private static string GetDefaultSettingsFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, "ToDoApp", "settings.json");
    }
}

public sealed class SettingsSaveFailedEventArgs(string errorMessage) : EventArgs
{
    public string ErrorMessage { get; } = errorMessage;
}
