using System;
using Microsoft.Win32;
using ToDoapp.Services;

namespace ToDoapp.Services;

public class StartupService
{
    private static StartupService? _instance;
    public static StartupService Instance => _instance ??= new StartupService();

    private const string AppName = "ToDoApp";
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private StartupService() { }

    public bool IsAutoStartEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
                return key?.GetValue(AppName) != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检查开机自启动失败: {ex.Message}");
                return false;
            }
        }
    }

    public void EnableAutoStart()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath)) return;

            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.SetValue(AppName, $"\"{exePath}\" --autostart");
            
            SettingsService.Instance.Settings.AutoStart = true;
            SettingsService.Instance.SaveSettings();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"启用开机自启动失败: {ex.Message}");
        }
    }

    public void DisableAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            key?.DeleteValue(AppName, false);
            
            SettingsService.Instance.Settings.AutoStart = false;
            SettingsService.Instance.SaveSettings();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"禁用开机自启动失败: {ex.Message}");
        }
    }

    public void SetAutoStart(bool enabled)
    {
        if (enabled)
        {
            EnableAutoStart();
        }
        else
        {
            DisableAutoStart();
        }
    }

    public void SyncWithSettings()
    {
        var settingsAutoStart = SettingsService.Instance.Settings.AutoStart;
        var registryAutoStart = IsAutoStartEnabled;

        if (settingsAutoStart != registryAutoStart)
        {
            if (settingsAutoStart)
            {
                EnableAutoStart();
            }
            else
            {
                DisableAutoStart();
            }
        }
    }
}
