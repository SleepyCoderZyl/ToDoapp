using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ToDoapp.Models;
using ToDoapp.Services;

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
        var opacityContent = CreateOpacitySettingContent();
        var startupContent = CreateStartupSettingContent();
        var startupReminderContent = CreateStartupReminderSettingContent();
        var hotkeyContent = CreateHotKeySettingContent();
        var alwaysOnTopContent = CreateAlwaysOnTopSettingContent();
        var startInWidgetModeContent = CreateStartInWidgetModeSettingContent();

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
            Description = "设置快速添加待办事项",
            Category = SettingCategory.General,
            ContentControl = hotkeyContent
        };

        var startupReminderItem = new SettingItem
        {
            Id = "startupReminder",
            Name = "开机提示",
            Description = "配置开机自启时显示的自定义提醒",
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

    private FrameworkElement CreateOpacitySettingContent()
    {
        var container = new Grid { Margin = new Thickness(20) };
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = "透明度设置",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(titleText, 0);
        container.Children.Add(titleText);

        var descriptionText = new TextBlock
        {
            Text = "分别调整小组件的背景和内容透明度。",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(descriptionText, 1);
        container.Children.Add(descriptionText);

        var backgroundLabel = new TextBlock
        {
            Text = "背景透明度",
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
            Margin = new Thickness(0, 10, 0, 10)
        };
        Grid.SetRow(backgroundLabel, 2);
        container.Children.Add(backgroundLabel);

        var backgroundSliderPanel = new Grid { Margin = new Thickness(0, 0, 0, 15) };
        backgroundSliderPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        backgroundSliderPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var backgroundSlider = new Slider
        {
            Minimum = 0.2,
            Maximum = 1.0,
            Value = WidgetOpacityManager.Instance.WidgetOpacity,
            Width = 300,
            Foreground = new SolidColorBrush(Color.FromRgb(99, 102, 241)),
            SmallChange = 0.1,
            LargeChange = 0.1,
            IsMoveToPointEnabled = true,
            TickFrequency = 0.1,
            TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight,
            IsSnapToTickEnabled = true,
            VerticalAlignment = VerticalAlignment.Center
        };



        var backgroundOpacityValueText = new TextBlock
        {
            Text = $"{Math.Round(WidgetOpacityManager.Instance.WidgetOpacity * 100)}%",
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            Width = 50,
            TextAlignment = TextAlignment.Left,
            Margin = new Thickness(15, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        backgroundSlider.ValueChanged += (s, e) =>
        {
            var value = (int)Math.Round(e.NewValue * 100);
            backgroundOpacityValueText.Text = $"{value}%";
            WidgetOpacityManager.Instance.SetOpacity(e.NewValue);
        };

        Grid.SetColumn(backgroundSlider, 0);
        Grid.SetColumn(backgroundOpacityValueText, 1);
        backgroundSliderPanel.Children.Add(backgroundSlider);
        backgroundSliderPanel.Children.Add(backgroundOpacityValueText);

        Grid.SetRow(backgroundSliderPanel, 3);
        container.Children.Add(backgroundSliderPanel);

        var contentLabel = new TextBlock
        {
            Text = "内容透明度",
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
            Margin = new Thickness(0, 15, 0, 10)
        };
        Grid.SetRow(contentLabel, 4);
        container.Children.Add(contentLabel);

        var contentSliderPanel = new Grid { Margin = new Thickness(0, 0, 0, 0) };
        contentSliderPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        contentSliderPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var contentSlider = new Slider
        {
            Minimum = 0.2,
            Maximum = 1.0,
            Value = WidgetOpacityManager.Instance.WidgetContentOpacity,
            Width = 300,
            Foreground = new SolidColorBrush(Color.FromRgb(99, 102, 241)),
            SmallChange = 0.1,
            LargeChange = 0.1,
            IsMoveToPointEnabled = true,
            TickFrequency = 0.1,
            TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight,
            IsSnapToTickEnabled = true,
            VerticalAlignment = VerticalAlignment.Center
        };



        var contentOpacityValueText = new TextBlock
        {
            Text = $"{Math.Round(WidgetOpacityManager.Instance.WidgetContentOpacity * 100)}%",
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            Width = 50,
            TextAlignment = TextAlignment.Left,
            Margin = new Thickness(15, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        contentSlider.ValueChanged += (s, e) =>
        {
            var value = (int)Math.Round(e.NewValue * 100);
            contentOpacityValueText.Text = $"{value}%";
            WidgetOpacityManager.Instance.SetContentOpacity(e.NewValue);
        };

        Grid.SetColumn(contentSlider, 0);
        Grid.SetColumn(contentOpacityValueText, 1);
        contentSliderPanel.Children.Add(contentSlider);
        contentSliderPanel.Children.Add(contentOpacityValueText);

        Grid.SetRow(contentSliderPanel, 5);
        container.Children.Add(contentSliderPanel);

        return container;
    }

    private FrameworkElement CreateStartupSettingContent()
    {
        var container = new Grid { Margin = new Thickness(20) };
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = "开机自启动",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(titleText, 0);
        container.Children.Add(titleText);

        var descriptionText = new TextBlock
        {
            Text = "启用后，应用程序将在 Windows 启动时自动运行。",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(descriptionText, 1);
        container.Children.Add(descriptionText);

        var togglePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var toggleSwitch = new CheckBox
        {
            Name = "StartupToggle",
            IsChecked = StartupService.Instance.IsAutoStartEnabled,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var statusText = new TextBlock
        {
            Name = "StartupStatusText",
            Text = StartupService.Instance.IsAutoStartEnabled ? "已启用" : "已禁用",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        toggleSwitch.Checked += (s, e) =>
        {
            StartupService.Instance.SetAutoStart(true);
            statusText.Text = "已启用";
        };

        toggleSwitch.Unchecked += (s, e) =>
        {
            StartupService.Instance.SetAutoStart(false);
            statusText.Text = "已禁用";
        };

        togglePanel.Children.Add(toggleSwitch);
        togglePanel.Children.Add(statusText);

        Grid.SetRow(togglePanel, 2);
        container.Children.Add(togglePanel);

        return container;
    }

    private FrameworkElement CreateStartupReminderSettingContent()
    {
        var settings = SettingsService.Instance.Settings;
        settings.StartupReminderItems ??= [];

        var reminderItems = settings.StartupReminderItems;
        var container = new Grid { Margin = new Thickness(20) };
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = "开机提示",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(titleText, 0);
        container.Children.Add(titleText);

        var descriptionText = new TextBlock
        {
            Text = "仅在开机自启时弹出提示窗口，展示你配置的自定义提醒。",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(descriptionText, 1);
        container.Children.Add(descriptionText);

        var togglePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var toggleSwitch = new CheckBox
        {
            IsChecked = settings.ShowStartupReminderOnAutoStart,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var statusText = new TextBlock
        {
            Text = settings.ShowStartupReminderOnAutoStart ? "已启用" : "已禁用",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        toggleSwitch.Checked += (s, e) =>
        {
            settings.ShowStartupReminderOnAutoStart = true;
            statusText.Text = "已启用";
            SettingsService.Instance.SaveSettings();
        };

        toggleSwitch.Unchecked += (s, e) =>
        {
            settings.ShowStartupReminderOnAutoStart = false;
            statusText.Text = "已禁用";
            SettingsService.Instance.SaveSettings();
        };

        togglePanel.Children.Add(toggleSwitch);
        togglePanel.Children.Add(statusText);
        Grid.SetRow(togglePanel, 2);
        container.Children.Add(togglePanel);

        var sectionTitle = new TextBlock
        {
            Text = "自定义提醒列表",
            FontSize = 14,
            FontWeight = FontWeights.Medium,
            Foreground = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
            Margin = new Thickness(0, 22, 0, 10)
        };
        Grid.SetRow(sectionTitle, 3);
        container.Children.Add(sectionTitle);

        var inputPanel = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        inputPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inputPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var inputTextBox = new TextBox
        {
            Style = Application.Current.Resources["ModernTextBoxStyle"] as Style,
            MinWidth = 220,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var addButton = new Button
        {
            Content = "新增提醒",
            Style = Application.Current.Resources["ModernButtonStyle"] as Style,
            Padding = new Thickness(16, 10, 16, 10)
        };

        Grid.SetColumn(inputTextBox, 0);
        Grid.SetColumn(addButton, 1);
        inputPanel.Children.Add(inputTextBox);
        inputPanel.Children.Add(addButton);

        Grid.SetRow(inputPanel, 4);
        container.Children.Add(inputPanel);

        var listScrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 220
        };

        var reminderListPanel = new StackPanel();
        listScrollViewer.Content = reminderListPanel;
        Grid.SetRow(listScrollViewer, 5);
        container.Children.Add(listScrollViewer);

        var hintText = new TextBlock
        {
            Text = "提示窗口会按这里的顺序展示已启用的提醒项，例如：上班打卡、写日报、带工牌。",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0)
        };
        Grid.SetRow(hintText, 6);
        container.Children.Add(hintText);

        void SaveReminderSettings()
        {
            settings.StartupReminderItems = reminderItems;
            SettingsService.Instance.SaveSettings();
        }

        void RefreshReminderList()
        {
            reminderListPanel.Children.Clear();

            if (reminderItems.Count == 0)
            {
                reminderListPanel.Children.Add(new TextBlock
                {
                    Text = "还没有自定义提醒，新增一条试试。",
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                    Margin = new Thickness(0, 8, 0, 0)
                });
                return;
            }

            foreach (var item in reminderItems)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var enabledBox = new CheckBox
                {
                    IsChecked = item.IsEnabled,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 12, 0)
                };

                var textBlock = new TextBlock
                {
                    Text = item.Text,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
                    TextWrapping = TextWrapping.Wrap
                };

                var deleteButton = new Button
                {
                    Content = "删除",
                    Style = Application.Current.Resources["SecondaryButtonStyle"] as Style,
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(12, 0, 0, 0)
                };

                enabledBox.Checked += (s, e) =>
                {
                    item.IsEnabled = true;
                    SaveReminderSettings();
                };

                enabledBox.Unchecked += (s, e) =>
                {
                    item.IsEnabled = false;
                    SaveReminderSettings();
                };

                deleteButton.Click += (s, e) =>
                {
                    reminderItems.Remove(item);
                    SaveReminderSettings();
                    RefreshReminderList();
                };

                Grid.SetColumn(enabledBox, 0);
                Grid.SetColumn(textBlock, 1);
                Grid.SetColumn(deleteButton, 2);
                row.Children.Add(enabledBox);
                row.Children.Add(textBlock);
                row.Children.Add(deleteButton);

                reminderListPanel.Children.Add(row);
            }
        }

        void AddReminder()
        {
            var text = inputTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            reminderItems.Add(new StartupReminderEntry
            {
                Text = text,
                IsEnabled = true
            });

            inputTextBox.Clear();
            SaveReminderSettings();
            RefreshReminderList();
        }

        addButton.Click += (s, e) => AddReminder();
        inputTextBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                AddReminder();
                e.Handled = true;
            }
        };

        RefreshReminderList();
        return container;
    }

    private FrameworkElement CreateHotKeySettingContent()
    {
        var container = new Grid { Margin = new Thickness(20) };
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = "全局快捷键",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(titleText, 0);
        container.Children.Add(titleText);

        var descriptionText = new TextBlock
        {
            Text = "设置快速添加待办事项的全局快捷键，按下快捷键时会显示快速添加窗口。",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(descriptionText, 1);
        container.Children.Add(descriptionText);

        var hotkeyPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 10, 0, 12) };

        var hotkeyText = new TextBlock
        {
            Text = "当前快捷键：",
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var hotkeyValueText = new TextBlock
        {
            Name = "HotkeyValueText",
            Text = Services.GlobalHotKeyService.GetHotKeyDisplayText(
                SettingsService.Instance.Settings.HotKeyModifiers,
                SettingsService.Instance.Settings.HotKeyKey),
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(99, 102, 241)),
            FontWeight = FontWeights.Medium,
            VerticalAlignment = VerticalAlignment.Center
        };

        hotkeyPanel.Children.Add(hotkeyText);
        hotkeyPanel.Children.Add(hotkeyValueText);

        Grid.SetRow(hotkeyPanel, 2);
        container.Children.Add(hotkeyPanel);

        var inputPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) };

        var inputTextBox = new TextBox
        {
            Name = "HotkeyInputTextBox",
            Style = Application.Current.Resources["ModernTextBoxStyle"] as Style,
            Width = 200,
            IsReadOnly = true,
            Text = "点击此处设置快捷键",
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = System.Windows.Input.Cursors.Hand
        };

        var resetButton = new Button
        {
            Name = "ResetHotkeyButton",
            Content = "重置",
            Style = Application.Current.Resources["SecondaryButtonStyle"] as Style,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        inputPanel.Children.Add(inputTextBox);
        inputPanel.Children.Add(resetButton);

        Grid.SetRow(inputPanel, 3);
        container.Children.Add(inputPanel);

        var statusText = new TextBlock
        {
            Name = "HotkeyStatusText",
            Text = "点击输入框设置新快捷键",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            Margin = new Thickness(0, 10, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetRow(statusText, 4);
        container.Children.Add(statusText);

        bool isRecording = false;

        inputTextBox.PreviewMouseLeftButtonDown += (s, e) =>
        {
            if (!isRecording)
            {
                isRecording = true;
                inputTextBox.Text = "按下快捷键组合...";
                inputTextBox.Foreground = new SolidColorBrush(Color.FromRgb(99, 102, 241));
                statusText.Text = "请按下想要的快捷键组合（至少包含一个修饰键），按 Esc 取消";
                inputTextBox.Focus();
            }
        };

        inputTextBox.PreviewKeyDown += (s2, e2) =>
        {
            if (!isRecording) return;
            e2.Handled = true;

            uint modifiers = 0;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers |= Services.GlobalHotKeyService.MOD_CONTROL;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers |= Services.GlobalHotKeyService.MOD_SHIFT;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers |= Services.GlobalHotKeyService.MOD_ALT;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) modifiers |= Services.GlobalHotKeyService.MOD_WIN;

            uint key = 0;
            if (e2.Key >= Key.A && e2.Key <= Key.Z)
                key = (uint)(0x41 + (e2.Key - Key.A));
            else if (e2.Key >= Key.D0 && e2.Key <= Key.D9)
                key = (uint)(0x30 + (e2.Key - Key.D0));
            else if (e2.Key >= Key.NumPad0 && e2.Key <= Key.NumPad9)
                key = (uint)(0x60 + (e2.Key - Key.NumPad0));
            else if (e2.Key >= Key.F1 && e2.Key <= Key.F24)
                key = (uint)(0x70 + (e2.Key - Key.F1));
            else if (e2.Key == Key.Space)
                key = Services.GlobalHotKeyService.VK_SPACE;
            else if (e2.Key == Key.Back)
                key = Services.GlobalHotKeyService.VK_BACK;
            else if (e2.Key == Key.Tab)
                key = Services.GlobalHotKeyService.VK_TAB;
            else if (e2.Key == Key.Return)
                key = Services.GlobalHotKeyService.VK_RETURN;
            else if (e2.Key == Key.Escape)
            {
                isRecording = false;
                inputTextBox.Text = "点击此处重新设置快捷键";
                inputTextBox.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                statusText.Text = "已取消，点击输入框可重新设置";
                return;
            }
            else if (e2.Key == Key.Home)
                key = Services.GlobalHotKeyService.VK_HOME;
            else if (e2.Key == Key.End)
                key = Services.GlobalHotKeyService.VK_END;
            else if (e2.Key == Key.Left)
                key = Services.GlobalHotKeyService.VK_LEFT;
            else if (e2.Key == Key.Up)
                key = Services.GlobalHotKeyService.VK_UP;
            else if (e2.Key == Key.Right)
                key = Services.GlobalHotKeyService.VK_RIGHT;
            else if (e2.Key == Key.Down)
                key = Services.GlobalHotKeyService.VK_DOWN;
            else if (e2.Key == Key.Insert)
                key = Services.GlobalHotKeyService.VK_INSERT;
            else if (e2.Key == Key.Delete)
                key = Services.GlobalHotKeyService.VK_DELETE;
            else if (e2.Key == Key.PageUp)
                key = Services.GlobalHotKeyService.VK_PRIOR;
            else if (e2.Key == Key.PageDown)
                key = Services.GlobalHotKeyService.VK_NEXT;

            if (modifiers != 0 && key != 0)
            {
                isRecording = false;
                SettingsService.Instance.Settings.HotKeyModifiers = modifiers;
                SettingsService.Instance.Settings.HotKeyKey = key;
                SettingsService.Instance.SaveSettings();
                hotkeyValueText.Text = Services.GlobalHotKeyService.GetHotKeyDisplayText(modifiers, key);
                inputTextBox.Text = "点击此处重新设置快捷键";
                inputTextBox.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                statusText.Text = "快捷键已更新，点击输入框可再次修改";
                inputTextBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            }
            else if (modifiers != 0)
            {
                inputTextBox.Text = Services.GlobalHotKeyService.GetHotKeyDisplayText(modifiers, 0) + "+...";
            }
        };

        inputTextBox.LostFocus += (s, e) =>
        {
            if (isRecording)
            {
                isRecording = false;
                inputTextBox.Text = "点击此处重新设置快捷键";
                inputTextBox.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
                statusText.Text = "快捷键已更新，点击输入框可再次修改";
            }
        };

        resetButton.Click += (s, e) =>
        {
            uint defaultModifiers = Services.GlobalHotKeyService.MOD_CONTROL | Services.GlobalHotKeyService.MOD_SHIFT | Services.GlobalHotKeyService.MOD_ALT;
            uint defaultKey = Services.GlobalHotKeyService.VK_Z;

            SettingsService.Instance.Settings.HotKeyModifiers = defaultModifiers;
            SettingsService.Instance.Settings.HotKeyKey = defaultKey;
            SettingsService.Instance.SaveSettings();

            hotkeyValueText.Text = Services.GlobalHotKeyService.GetHotKeyDisplayText(defaultModifiers, defaultKey);
            inputTextBox.Text = "点击此处重新设置快捷键";
            inputTextBox.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175));
            statusText.Text = "快捷键已重置为默认值，点击输入框可修改";
        };

        return container;
    }

    private FrameworkElement CreateAlwaysOnTopSettingContent()
    {
        var container = new Grid { Margin = new Thickness(20) };
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = "小组件置顶",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(titleText, 0);
        container.Children.Add(titleText);

        var descriptionText = new TextBlock
        {
            Text = "启用后，小组件将始终显示在所有其他窗口的最上层。",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(descriptionText, 1);
        container.Children.Add(descriptionText);

        var togglePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var toggleSwitch = new CheckBox
        {
            Name = "AlwaysOnTopToggle",
            IsChecked = SettingsService.Instance.Settings.WidgetAlwaysOnTop,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var statusText = new TextBlock
        {
            Name = "AlwaysOnTopStatusText",
            Text = SettingsService.Instance.Settings.WidgetAlwaysOnTop ? "已启用" : "已禁用",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        toggleSwitch.Checked += (s, e) =>
        {
            SettingsService.Instance.UpdateWidgetAlwaysOnTop(true);
            statusText.Text = "已启用";
        };

        toggleSwitch.Unchecked += (s, e) =>
        {
            SettingsService.Instance.UpdateWidgetAlwaysOnTop(false);
            statusText.Text = "已禁用";
        };

        togglePanel.Children.Add(toggleSwitch);
        togglePanel.Children.Add(statusText);

        Grid.SetRow(togglePanel, 2);
        container.Children.Add(togglePanel);

        return container;
    }

    private FrameworkElement CreateStartInWidgetModeSettingContent()
    {
        var container = new Grid { Margin = new Thickness(20) };
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = "启动模式",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(titleText, 0);
        container.Children.Add(titleText);

        var descriptionText = new TextBlock
        {
            Text = "启用后，程序启动时将自动进入小组件模式，不显示主页面。",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(descriptionText, 1);
        container.Children.Add(descriptionText);

        var togglePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        var toggleSwitch = new CheckBox
        {
            Name = "StartInWidgetModeToggle",
            IsChecked = SettingsService.Instance.Settings.StartInWidgetMode,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var statusText = new TextBlock
        {
            Name = "StartInWidgetModeStatusText",
            Text = SettingsService.Instance.Settings.StartInWidgetMode ? "小组件模式" : "主页面模式",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        toggleSwitch.Checked += (s, e) =>
        {
            SettingsService.Instance.Settings.StartInWidgetMode = true;
            SettingsService.Instance.SaveSettings();
            statusText.Text = "小组件模式";
        };

        toggleSwitch.Unchecked += (s, e) =>
        {
            SettingsService.Instance.Settings.StartInWidgetMode = false;
            SettingsService.Instance.SaveSettings();
            statusText.Text = "主页面模式";
        };

        togglePanel.Children.Add(toggleSwitch);
        togglePanel.Children.Add(statusText);

        Grid.SetRow(togglePanel, 2);
        container.Children.Add(togglePanel);

        return container;
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
