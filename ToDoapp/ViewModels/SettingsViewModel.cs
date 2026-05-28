using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ToDoapp.Models;
using ToDoapp.Services;
using ToDoapp.Views;
namespace ToDoapp.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private SettingItem? _selectedSettingItem;
    private FrameworkElement? _currentContent;

    public ObservableCollection<SettingItem> SettingItems { get; } = new();
    public ObservableCollection<SettingGroup> SettingGroups { get; } = new();

    public SettingItem? SelectedSettingItem
    {
        get => _selectedSettingItem;
        set
        {
            if (_selectedSettingItem != value)
            {
                _selectedSettingItem = value;
                OnPropertyChanged();
                UpdateCurrentContent();
            }
        }
    }

    public FrameworkElement? CurrentContent
    {
        get => _currentContent;
        private set
        {
            _currentContent = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsViewModel()
    {
        InitializeSettingItems();
    }

    private void InitializeSettingItems()
    {
        var opacityContent = SettingsContentFactory.CreateOpacitySettingContent();
        var startupContent = SettingsContentFactory.CreateStartupSettingContent();
        var startupReminderContent = SettingsContentFactory.CreateStartupReminderSettingContent();
        var hotkeyContent = SettingsContentFactory.CreateHotKeySettingContent();
        var alwaysOnTopContent = SettingsContentFactory.CreateAlwaysOnTopSettingContent();
        var startInWidgetModeContent = SettingsContentFactory.CreateStartInWidgetModeSettingContent();

        var generalGroup = new SettingGroup
        {
            Name = "常规",
            Category = SettingCategory.General
        };

        var appearanceGroup = new SettingGroup
        {
            Name = "外观",
            Category = SettingCategory.Appearance
        };

        var opacityItem = new SettingItem
        {
            Id = "opacity",
            Name = "透明度设置",
            Description = "调整小组件的透明度",
            Category = SettingCategory.Appearance,
            ContentControl = opacityContent
        };

        var alwaysOnTopItem = new SettingItem
        {
            Id = "alwaysOnTop",
            Name = "小组件置顶",
            Description = "使小组件始终显示在最上层",
            Category = SettingCategory.Appearance,
            ContentControl = alwaysOnTopContent
        };

        var startupItem = new SettingItem
        {
            Id = "startup",
            Name = "开机自启动",
            Description = "开机时自动启动应用程序",
            Category = SettingCategory.General,
            ContentControl = startupContent
        };

        var hotkeyItem = new SettingItem
        {
            Id = "hotkey",
            Name = "全局快捷键",
            Description = "设置快速添加和显示主页",
            Category = SettingCategory.General,
            ContentControl = hotkeyContent
        };

        var startupReminderItem = new SettingItem
        {
            Id = "startupReminder",
            Name = "弹窗提示",
            Description = "配置启动与定时弹出的提醒内容",
            Category = SettingCategory.General,
            ContentControl = startupReminderContent
        };

        var startInWidgetModeItem = new SettingItem
        {
            Id = "startInWidgetMode",
            Name = "启动模式",
            Description = "启动时自动进入小组件模式",
            Category = SettingCategory.General,
            ContentControl = startInWidgetModeContent
        };

        generalGroup.Items.Add(startupItem);
        generalGroup.Items.Add(startupReminderItem);
        generalGroup.Items.Add(startInWidgetModeItem);
        generalGroup.Items.Add(hotkeyItem);
        appearanceGroup.Items.Add(opacityItem);
        appearanceGroup.Items.Add(alwaysOnTopItem);

        SettingGroups.Add(generalGroup);
        SettingGroups.Add(appearanceGroup);

        foreach (var group in SettingGroups)
        {
            foreach (var item in group.Items)
            {
                SettingItems.Add(item);
            }
        }

        if (SettingItems.Count > 0)
        {
            SelectedSettingItem = SettingItems[0];
        }
    }

    private void UpdateCurrentContent()
    {
        if (SelectedSettingItem?.ContentControl != null)
        {
            CurrentContent = SelectedSettingItem.ContentControl;
        }
        else
        {
            CurrentContent = null;
        }
    }

    public void SelectSettingItem(SettingItem item)
    {
        SelectedSettingItem = item;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
public class SettingGroup
{
    public string Name { get; set; } = string.Empty;
    public SettingCategory Category { get; set; }
    public ObservableCollection<SettingItem> Items { get; } = new();
}
