using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using HandyControl.Data;
using HandyControl.Tools;
using Microsoft.Win32;

namespace ToDoapp.Services;

public sealed class ThemeService
{
    private const string RegistryThemeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValue = "AppsUseLightTheme";
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
        catch
        {
            return true;
        }
    }

    private void ApplyTheme(bool useDarkTheme)
    {
        IsDarkTheme = useDarkTheme;
        var palette = useDarkTheme ? DarkPalette : LightPalette;

        ApplyHandyControlTheme(useDarkTheme);

        foreach (var (key, color) in palette)
        {
            UpdateColorResource(key, color);
            UpdateBrushResource(GetBrushKey(key), color);
        }

        foreach (var (key, color) in useDarkTheme ? DarkBrushes : LightBrushes)
        {
            UpdateBrushResource(key, color);
        }

        UpdatePrimaryGradient(palette["PrimaryColor"], palette["PrimaryDarkColor"]);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void UpdateColorResource(string key, Color color)
    {
        UpdateColorResource(Application.Current.Resources, key, color);
    }

    private static void UpdateBrushResource(string key, Color color)
    {
        UpdateBrushResource(Application.Current.Resources, key, color);
    }

    private static bool UpdateColorResource(ResourceDictionary dictionary, string key, Color color)
    {
        var updated = false;
        if (dictionary.Contains(key))
        {
            dictionary[key] = color;
            updated = true;
        }

        foreach (var childDictionary in dictionary.MergedDictionaries)
        {
            updated |= UpdateColorResource(childDictionary, key, color);
        }

        return updated;
    }

    private static bool UpdateBrushResource(ResourceDictionary dictionary, string key, Color color)
    {
        var updated = false;
        if (dictionary.Contains(key))
        {
            if (dictionary[key] is SolidColorBrush brush && !brush.IsFrozen)
            {
                brush.Color = color;
            }
            else
            {
                dictionary[key] = new SolidColorBrush(color);
            }

            updated = true;
        }

        foreach (var childDictionary in dictionary.MergedDictionaries)
        {
            updated |= UpdateBrushResource(childDictionary, key, color);
        }

        return updated;
    }

    private static string GetBrushKey(string colorKey)
    {
        return colorKey.EndsWith("Color", StringComparison.Ordinal)
            ? colorKey[..^"Color".Length] + "Brush"
            : colorKey + "Brush";
    }

    private static ResourceDictionary? FindResourceDictionary(ResourceDictionary dictionary, string key)
    {
        if (dictionary.Contains(key))
        {
            return dictionary;
        }

        foreach (var childDictionary in dictionary.MergedDictionaries)
        {
            var found = FindResourceDictionary(childDictionary, key);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void UpdatePrimaryGradient(Color primaryColor, Color primaryDarkColor)
    {
        var dictionary = FindResourceDictionary(Application.Current.Resources, "PrimaryGradientBrush");
        if (dictionary == null)
        {
            return;
        }

        dictionary["PrimaryGradientBrush"] = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            [
                new GradientStop(primaryColor, 0),
                new GradientStop(primaryDarkColor, 1)
            ]
        };
    }

    private static void ApplyHandyControlTheme(bool useDarkTheme)
    {
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var skinDictionary = dictionaries.FirstOrDefault(dictionary =>
            dictionary.Source?.OriginalString.Contains("HandyControl;component/Themes/Skin", StringComparison.OrdinalIgnoreCase) == true);
        var themeDictionary = dictionaries.FirstOrDefault(dictionary =>
            dictionary.Source?.OriginalString.Contains("HandyControl;component/Themes/Theme.xaml", StringComparison.OrdinalIgnoreCase) == true);

        if (skinDictionary == null)
        {
            return;
        }

        try
        {
            var skinIndex = dictionaries.IndexOf(skinDictionary);
            dictionaries[skinIndex] = ResourceHelper.GetSkin(useDarkTheme ? SkinType.Dark : SkinType.Default);

            if (themeDictionary != null)
            {
                var themeIndex = dictionaries.IndexOf(themeDictionary);
                dictionaries[themeIndex] = ResourceHelper.GetTheme();
            }
            else
            {
                dictionaries.Insert(skinIndex + 1, ResourceHelper.GetTheme());
            }
        }
        catch
        {
        }
    }

    private static readonly IReadOnlyDictionary<string, Color> DarkPalette = new Dictionary<string, Color>
    {
        ["PrimaryColor"] = Color.FromRgb(255, 107, 71),
        ["PrimaryLightColor"] = Color.FromRgb(255, 130, 95),
        ["PrimaryDarkColor"] = Color.FromRgb(232, 90, 54),
        ["DarkPrimaryColor"] = Color.FromRgb(232, 90, 54),
        ["AccentColor"] = Color.FromRgb(255, 177, 153),
        ["DarkAccentColor"] = Color.FromRgb(232, 90, 54),
        ["SuccessColor"] = Color.FromRgb(94, 194, 105),
        ["DangerColor"] = Color.FromRgb(248, 81, 73),
        ["WarningColor"] = Color.FromRgb(242, 184, 75),
        ["BackgroundDark"] = Color.FromRgb(32, 32, 32),
        ["BackgroundMedium"] = Color.FromRgb(45, 45, 45),
        ["BackgroundLight"] = Color.FromRgb(61, 61, 61),
        ["SurfaceColor"] = Color.FromRgb(31, 31, 31),
        ["SurfaceLight"] = Color.FromRgb(40, 40, 40),
        ["TextPrimary"] = Color.FromRgb(245, 245, 245),
        ["TextSecondary"] = Color.FromRgb(184, 184, 184),
        ["TextMuted"] = Color.FromRgb(122, 122, 122)
    };

    private static readonly IReadOnlyDictionary<string, Color> LightPalette = new Dictionary<string, Color>
    {
        ["PrimaryColor"] = Color.FromRgb(255, 107, 71),
        ["PrimaryLightColor"] = Color.FromRgb(255, 130, 95),
        ["PrimaryDarkColor"] = Color.FromRgb(232, 90, 54),
        ["DarkPrimaryColor"] = Color.FromRgb(232, 90, 54),
        ["AccentColor"] = Color.FromRgb(255, 177, 153),
        ["DarkAccentColor"] = Color.FromRgb(232, 90, 54),
        ["SuccessColor"] = Color.FromRgb(43, 154, 70),
        ["DangerColor"] = Color.FromRgb(214, 59, 49),
        ["WarningColor"] = Color.FromRgb(196, 126, 35),
        ["BackgroundDark"] = Color.FromRgb(247, 247, 247),
        ["BackgroundMedium"] = Color.FromRgb(243, 243, 243),
        ["BackgroundLight"] = Color.FromRgb(208, 208, 208),
        ["SurfaceColor"] = Color.FromRgb(255, 255, 255),
        ["SurfaceLight"] = Color.FromRgb(243, 243, 243),
        ["TextPrimary"] = Color.FromRgb(26, 26, 26),
        ["TextSecondary"] = Color.FromRgb(82, 82, 82),
        ["TextMuted"] = Color.FromRgb(118, 118, 118)
    };

    private static readonly IReadOnlyDictionary<string, Color> DarkBrushes = new Dictionary<string, Color>
    {
        ["DangerSubtleBrush"] = Color.FromArgb(51, 248, 81, 73),
        ["TextOnPrimaryBrush"] = Colors.White,
        ["WindowBackgroundBrush"] = Color.FromRgb(32, 32, 32),
        ["TitleBarBrush"] = Color.FromRgb(31, 31, 31),
        ["SidebarBrush"] = Color.FromRgb(40, 40, 40),
        ["ContentBackgroundBrush"] = Color.FromRgb(32, 32, 32),
        ["PanelBrush"] = Color.FromRgb(40, 40, 40),
        ["PanelAltBrush"] = Color.FromRgb(45, 45, 45),
        ["CardBrush"] = Color.FromRgb(36, 36, 36),
        ["InputBrush"] = Color.FromRgb(36, 36, 36),
        ["HoverBrush"] = Color.FromRgb(52, 52, 52),
        ["PressedBrush"] = Color.FromRgb(31, 31, 31),
        ["BorderBrush"] = Color.FromRgb(61, 61, 61),
        ["SeparatorBrush"] = Color.FromRgb(51, 51, 51),
        ["PlaceholderBrush"] = Color.FromRgb(122, 122, 122),
        ["TagBrush"] = Color.FromRgb(48, 48, 48),
        ["WidgetBackgroundBrush"] = Color.FromRgb(32, 32, 32),
        ["TabSelectedBackgroundBrush"] = Color.FromRgb(52, 52, 52),
        ["TabSelectedBorderBrush"] = Color.FromRgb(255, 107, 71),
        ["TabSelectedForegroundBrush"] = Color.FromRgb(245, 245, 245),
        ["DialogBackgroundBrush"] = Color.FromRgb(32, 32, 32),
        ["DialogBorderBrush"] = Color.FromRgb(61, 61, 61),
        ["DialogForegroundBrush"] = Color.FromRgb(245, 245, 245),
        ["DialogSecondaryForegroundBrush"] = Color.FromRgb(184, 184, 184),
        ["PrimaryButtonBackgroundBrush"] = Color.FromRgb(255, 107, 71),
        ["PrimaryButtonHoverBrush"] = Color.FromRgb(255, 130, 95),
        ["PrimaryButtonPressedBrush"] = Color.FromRgb(232, 90, 54),
        ["SecondaryButtonBackgroundBrush"] = Color.FromRgb(45, 45, 45),
        ["SecondaryButtonHoverBrush"] = Color.FromRgb(61, 61, 61),
        ["SecondaryButtonPressedBrush"] = Color.FromRgb(31, 31, 31)
    };

    private static readonly IReadOnlyDictionary<string, Color> LightBrushes = new Dictionary<string, Color>
    {
        ["DangerSubtleBrush"] = Color.FromArgb(32, 214, 59, 49),
        ["TextOnPrimaryBrush"] = Colors.White,
        ["WindowBackgroundBrush"] = Color.FromRgb(247, 247, 247),
        ["TitleBarBrush"] = Color.FromRgb(255, 255, 255),
        ["SidebarBrush"] = Color.FromRgb(243, 243, 243),
        ["ContentBackgroundBrush"] = Color.FromRgb(247, 247, 247),
        ["PanelBrush"] = Color.FromRgb(255, 255, 255),
        ["PanelAltBrush"] = Color.FromRgb(243, 243, 243),
        ["CardBrush"] = Color.FromRgb(255, 255, 255),
        ["InputBrush"] = Color.FromRgb(255, 255, 255),
        ["HoverBrush"] = Color.FromRgb(242, 242, 242),
        ["PressedBrush"] = Color.FromRgb(230, 230, 230),
        ["BorderBrush"] = Color.FromRgb(208, 208, 208),
        ["SeparatorBrush"] = Color.FromRgb(229, 229, 229),
        ["PlaceholderBrush"] = Color.FromRgb(118, 118, 118),
        ["TagBrush"] = Color.FromRgb(242, 242, 242),
        ["WidgetBackgroundBrush"] = Color.FromRgb(255, 255, 255),
        ["TabSelectedBackgroundBrush"] = Color.FromRgb(255, 239, 233),
        ["TabSelectedBorderBrush"] = Color.FromRgb(255, 107, 71),
        ["TabSelectedForegroundBrush"] = Color.FromRgb(184, 61, 31),
        ["DialogBackgroundBrush"] = Color.FromRgb(255, 255, 255),
        ["DialogBorderBrush"] = Color.FromRgb(208, 208, 208),
        ["DialogForegroundBrush"] = Color.FromRgb(26, 26, 26),
        ["DialogSecondaryForegroundBrush"] = Color.FromRgb(82, 82, 82),
        ["PrimaryButtonBackgroundBrush"] = Color.FromRgb(255, 107, 71),
        ["PrimaryButtonHoverBrush"] = Color.FromRgb(255, 130, 95),
        ["PrimaryButtonPressedBrush"] = Color.FromRgb(232, 90, 54),
        ["SecondaryButtonBackgroundBrush"] = Color.FromRgb(243, 243, 243),
        ["SecondaryButtonHoverBrush"] = Color.FromRgb(232, 232, 232),
        ["SecondaryButtonPressedBrush"] = Color.FromRgb(218, 218, 218)
    };
}
