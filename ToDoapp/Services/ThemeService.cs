using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace ToDoapp.Services;

public sealed class ThemeService
{
    private const string RegistryThemeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValue = "AppsUseLightTheme";
    private const string DarkThemeUri = "pack://application:,,,/ToDoapp;component/Resources/Themes/DarkTheme.xaml";
    private const string LightThemeUri = "pack://application:,,,/ToDoapp;component/Resources/Themes/LightTheme.xaml";

    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();

    private bool _isSystemThemeHooked;

    public bool IsDarkTheme { get; private set; }

    public event EventHandler? ThemeChanged;

    private ThemeService()
    {
    }

    public void Initialize()
    {
        ApplyConfiguredTheme();
        if (!_isSystemThemeHooked)
        {
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            _isSystemThemeHooked = true;
        }
    }

    public void ApplyConfiguredTheme()
    {
        var mode = SettingsService.Instance.Settings.ThemeMode;
        var useDarkTheme = mode switch
        {
            "Light" => false,
            "Dark" => true,
            _ => IsSystemDarkTheme()
        };

        ApplyTheme(useDarkTheme);
    }

    public void ToggleExplicitTheme()
    {
        var nextMode = IsDarkTheme ? "Light" : "Dark";
        SettingsService.Instance.Settings.ThemeMode = nextMode;
        SettingsService.Instance.SaveSettings();
        ApplyTheme(nextMode == "Dark");
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General &&
            e.Category != UserPreferenceCategory.Color)
        {
            return;
        }

        if (SettingsService.Instance.Settings.ThemeMode == "System")
        {
            Application.Current.Dispatcher.Invoke(ApplyConfiguredTheme);
        }
    }

    private static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryThemeKey);
            var value = key?.GetValue(AppsUseLightThemeValue);
            return value is int intValue && intValue == 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"读取系统主题设置失败: {ex.Message}");
            return true;
        }
    }

    private void ApplyTheme(bool useDarkTheme)
    {
        IsDarkTheme = useDarkTheme;

        ReplaceThemeDictionary(useDarkTheme ? DarkThemeUri : LightThemeUri);

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void ReplaceThemeDictionary(string themeUri)
    {
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var themeDict = FindThemeDictionary(dictionaries);
        var newDict = new ResourceDictionary { Source = new Uri(themeUri) };

        if (themeDict != null)
        {
            var index = dictionaries.IndexOf(themeDict);
            dictionaries[index] = newDict;
        }
        else
        {
            dictionaries.Insert(0, newDict);
        }
    }

    private static ResourceDictionary? FindThemeDictionary(Collection<ResourceDictionary> dictionaries)
    {
        return dictionaries.FirstOrDefault(d =>
        {
            var source = d.Source?.OriginalString;
            return source != null &&
                   source.Contains("/Themes/", StringComparison.OrdinalIgnoreCase) &&
                   (source.Contains("DarkTheme", StringComparison.OrdinalIgnoreCase) ||
                    source.Contains("LightTheme", StringComparison.OrdinalIgnoreCase));
        });
    }
}
