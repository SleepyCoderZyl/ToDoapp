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
    private static FrameworkElement CreateStartupSettingContentCore()
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

    private static FrameworkElement CreateStartupReminderSettingContentCore()
    {
        var settings = SettingsService.Instance.Settings;
        settings.StartupReminderItems ??= [];
        settings.ScheduledReminderItems ??= [];
        if (string.IsNullOrWhiteSpace(settings.ScheduledReminderTime))
        {
            settings.ScheduledReminderTime = "09:00";
        }

        var container = new Grid { Margin = new Thickness(20) };
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var titleText = new TextBlock
        {
            Text = "弹窗提示",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(titleText, 0);
        container.Children.Add(titleText);

        var descriptionText = new TextBlock
        {
            Text = "统一管理应用启动和每日定时弹出的提醒内容。",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(descriptionText, 1);
        container.Children.Add(descriptionText);

        var tabControl = new TabControl
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Style = Application.Current.Resources["ModernTabControlStyle"] as Style
        };

        var tabItemStyle = Application.Current.Resources["ModernTabItemStyle"] as Style;
        if (tabItemStyle != null)
        {
            tabControl.Resources.Add(typeof(TabItem), new Style(typeof(TabItem), tabItemStyle));
        }

        tabControl.Items.Add(new TabItem
        {
            Header = "启动弹窗",
            Content = CreateReminderTabContent(
                description: "开机自启拉起时弹出你配置的提醒内容。",
                isEnabled: settings.ShowStartupReminderOnAutoStart,
                onEnabledChanged: isEnabled =>
                {
                    settings.ShowStartupReminderOnAutoStart = isEnabled;
                    SettingsService.Instance.SaveSettings();
                },
                reminderItems: settings.StartupReminderItems,
                onSaveItems: () =>
                {
                    settings.StartupReminderItems = settings.StartupReminderItems;
                    SettingsService.Instance.SaveSettings();
                },
                emptyStateText: "还没有启动弹窗内容，新增一条试试。",
                hintText: "提示窗口会按这里的顺序展示已启用的提醒项。")
        });

        var scheduledReminderTimeContent = CreateScheduledReminderTimeContent(settings);
        tabControl.Items.Add(new TabItem
        {
            Header = "定时弹窗",
            Content = CreateReminderTabContent(
                description: "应用运行或驻留托盘时，每天在指定时间弹出一次。",
                isEnabled: settings.ShowScheduledReminderDaily,
                onEnabledChanged: isEnabled =>
                {
                    settings.ShowScheduledReminderDaily = isEnabled;
                    SettingsService.Instance.SaveSettings();
                },
                reminderItems: settings.ScheduledReminderItems,
                onSaveItems: () =>
                {
                    settings.ScheduledReminderItems = settings.ScheduledReminderItems;
                    SettingsService.Instance.SaveSettings();
                },
                emptyStateText: "还没有定时提示内容，新增一条试试。",
                hintText: "每天只会在设定时间弹出一次，内容按这里的顺序展示已启用的提醒项。",
                inlineInputPrefixContent: scheduledReminderTimeContent.InputContent,
                extraSettingsContent: scheduledReminderTimeContent.HelperContent)
        });

        Grid.SetRow(tabControl, 2);
        container.Children.Add(tabControl);

        return container;
    }

    private static FrameworkElement CreateReminderTabContent(
        string description,
        bool isEnabled,
        Action<bool> onEnabledChanged,
        System.Collections.Generic.List<StartupReminderEntry> reminderItems,
        Action onSaveItems,
        string emptyStateText,
        string hintText,
        FrameworkElement? inlineInputPrefixContent = null,
        FrameworkElement? extraSettingsContent = null)
    {
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var container = new StackPanel
        {
            Margin = new Thickness(0, 8, 0, 0)
        };
        scrollViewer.Content = container;

        var descriptionText = new TextBlock
        {
            Text = description,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        };
        container.Children.Add(descriptionText);

        var togglePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var toggleSwitch = new CheckBox
        {
            IsChecked = isEnabled,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(229, 231, 235)),
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var statusText = new TextBlock
        {
            Text = isEnabled ? "已启用" : "已禁用",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        toggleSwitch.Checked += (s, e) =>
        {
            statusText.Text = "已启用";
            onEnabledChanged(true);
        };

        toggleSwitch.Unchecked += (s, e) =>
        {
            statusText.Text = "已禁用";
            onEnabledChanged(false);
        };

        togglePanel.Children.Add(toggleSwitch);
        togglePanel.Children.Add(statusText);
        container.Children.Add(togglePanel);

        if (extraSettingsContent != null)
        {
            container.Children.Add(extraSettingsContent);
        }

        var sectionTitle = new TextBlock
        {
            Text = "提醒内容",
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            Foreground = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
            Margin = new Thickness(0, 2, 0, 8)
        };
        container.Children.Add(sectionTitle);

        var inputPanel = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        if (inlineInputPrefixContent != null)
        {
            inputPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        inputPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inputPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var inputTextBox = new TextBox
        {
            Style = Application.Current.Resources["ModernTextBoxStyle"] as Style,
            MinWidth = 160,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var addButton = new Button
        {
            Content = "新增提醒",
            Style = Application.Current.Resources["ModernButtonStyle"] as Style,
            Padding = new Thickness(14, 10, 14, 10)
        };

        var inputTextColumn = inlineInputPrefixContent != null ? 1 : 0;
        var addButtonColumn = inlineInputPrefixContent != null ? 2 : 1;

        if (inlineInputPrefixContent != null)
        {
            inlineInputPrefixContent.Margin = new Thickness(0, 0, 10, 0);
            Grid.SetColumn(inlineInputPrefixContent, 0);
            inputPanel.Children.Add(inlineInputPrefixContent);
        }

        Grid.SetColumn(inputTextBox, inputTextColumn);
        Grid.SetColumn(addButton, addButtonColumn);
        inputPanel.Children.Add(inputTextBox);
        inputPanel.Children.Add(addButton);
        container.Children.Add(inputPanel);

        var listScrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 240
        };

        var reminderListPanel = new StackPanel();
        listScrollViewer.Content = reminderListPanel;
        container.Children.Add(listScrollViewer);

        var hintTextBlock = new TextBlock
        {
            Text = hintText,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0)
        };
        container.Children.Add(hintTextBlock);

        void SaveReminderSettings()
        {
            onSaveItems();
        }

        void RefreshReminderList()
        {
            reminderListPanel.Children.Clear();

            if (reminderItems.Count == 0)
            {
                reminderListPanel.Children.Add(new TextBlock
                {
                    Text = emptyStateText,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                    Margin = new Thickness(0, 6, 0, 0)
                });
                return;
            }

            foreach (var item in reminderItems)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
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
                    Margin = new Thickness(10, 0, 0, 0)
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
        return scrollViewer;
    }

    private static (FrameworkElement InputContent, FrameworkElement HelperContent) CreateScheduledReminderTimeContent(AppSettings settings)
    {
        if (!StartupReminderService.TryParseScheduledReminderTime(settings.ScheduledReminderTime, out var scheduledTime))
        {
            scheduledTime = new TimeOnly(9, 0);
            settings.ScheduledReminderTime = "09:00";
        }

        var inputContainer = new Grid
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        inputContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        inputContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        inputContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        inputContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var timeLabel = new TextBlock
        {
            Text = "时间",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        var timeInputBox = new TextBox
        {
            Width = 84,
            Text = scheduledTime.ToString("HH:mm"),
            MaxLength = 5,
            Style = Application.Current.Resources["ModernTextBoxStyle"] as Style,
            VerticalAlignment = VerticalAlignment.Center
        };

        var formatHintText = new TextBlock
        {
            Text = "HH:mm",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            Margin = new Thickness(8, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var timeHintText = new TextBlock
        {
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };

        void ShowTimeHint(string text, Brush? foreground = null)
        {
            timeHintText.Text = text;
            timeHintText.Foreground = foreground ?? new SolidColorBrush(Color.FromRgb(156, 163, 175));
        }

        void CommitScheduledTime()
        {
            var input = timeInputBox.Text.Trim();
            if (!StartupReminderService.TryParseScheduledReminderTime(input, out var parsedTime))
            {
                ShowTimeHint("请输入有效时间，例如 09:00。", new SolidColorBrush(Color.FromRgb(248, 81, 73)));
                return;
            }

            settings.ScheduledReminderTime = parsedTime.ToString("HH:mm");
            timeInputBox.Text = settings.ScheduledReminderTime;
            SettingsService.Instance.SaveSettings();
            ShowTimeHint($"每天 {settings.ScheduledReminderTime} 弹出一次");
        }

        timeInputBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                CommitScheduledTime();
                e.Handled = true;
            }
        };
        timeInputBox.LostFocus += (s, e) => CommitScheduledTime();

        Grid.SetColumn(timeLabel, 0);
        Grid.SetColumn(timeInputBox, 1);
        Grid.SetColumn(formatHintText, 2);
        inputContainer.Children.Add(timeLabel);
        inputContainer.Children.Add(timeInputBox);
        inputContainer.Children.Add(formatHintText);

        ShowTimeHint($"每天 {settings.ScheduledReminderTime} 弹出一次");

        var helperContainer = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 10)
        };
        helperContainer.Children.Add(timeHintText);
        helperContainer.Children.Add(new TextBlock
        {
            Text = "仅在应用运行或驻留托盘时生效；如果错过当天时间点，不会补发。",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 0)
        });

        return (inputContainer, helperContainer);
    }

    private static FrameworkElement CreateHotKeySettingContentCore()
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

    private static FrameworkElement CreateStartInWidgetModeSettingContentCore()
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


}

