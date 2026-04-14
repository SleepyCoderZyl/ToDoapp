using System;
using System.Windows;

namespace ToDoapp.Models;

public class SettingItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SettingCategory Category { get; set; } = SettingCategory.General;
    public FrameworkElement? ContentControl { get; set; }
    public Action<SettingItem>? OnSelected { get; set; }
}

public enum SettingCategory
{
    General,
    Appearance,
    Behavior,
    Shortcut,
    About
}
