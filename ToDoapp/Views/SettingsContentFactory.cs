using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.Views;

public static partial class SettingsContentFactory
{
    public static FrameworkElement CreateOpacitySettingContent() => CreateOpacitySettingContentCore();

    public static FrameworkElement CreateStartupSettingContent() => CreateStartupSettingContentCore();

    public static FrameworkElement CreateStartupReminderSettingContent() => CreateStartupReminderSettingContentCore();

    public static FrameworkElement CreateHotKeySettingContent() => CreateHotKeySettingContentCore();

    public static FrameworkElement CreateAlwaysOnTopSettingContent() => CreateAlwaysOnTopSettingContentCore();

    public static FrameworkElement CreateStartInWidgetModeSettingContent() => CreateStartInWidgetModeSettingContentCore();

}

