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
            Foreground = TextPrimaryBrush,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(titleText, 0);
        container.Children.Add(titleText);

        var descriptionText = new TextBlock
        {
            Text = "启用后，应用程序将在 Windows 启动时自动运行。",
            FontSize = 13,
            Foreground = TextSecondaryBrush,
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
            Foreground = TextPrimaryBrush,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var statusText = new TextBlock
        {
            Name = "StartupStatusText",
            Text = StartupService.Instance.IsAutoStartEnabled ? "已启用" : "已禁用",
            FontSize = 13,
            Foreground = TextSecondaryBrush,
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
        settings.Normalize();

        var container = new Grid { Margin = new Thickness(20) };
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var titleText = new TextBlock
        {
            Text = "弹窗提示",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextPrimaryBrush,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(titleText, 0);
        container.Children.Add(titleText);

        var descriptionText = new TextBlock
        {
            Text = "统一管理应用启动和每日定时弹出的提醒内容。",
            FontSize = 13,
            Foreground = TextSecondaryBrush,
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
                    SettingsService.Instance.SaveSettings();
                },
                emptyStateText: "还没有启动弹窗内容，新增一条试试。",
                hintText: "提示窗口会按这里的顺序展示已启用的提醒项。")
        });

        tabControl.Items.Add(new TabItem
        {
            Header = "定时弹窗",
            Content = CreateReminderTabContent(
                description: "应用运行或驻留托盘时，每条提醒会在各自设置的时间每天弹出一次。",
                isEnabled: settings.ShowScheduledReminderDaily,
                onEnabledChanged: isEnabled =>
                {
                    settings.ShowScheduledReminderDaily = isEnabled;
                    SettingsService.Instance.SaveSettings();
                },
                reminderItems: settings.ScheduledReminderItems,
                onSaveItems: () =>
                {
                    SettingsService.Instance.SaveSettings();
                },
                emptyStateText: "还没有定时提示内容，新增一条试试。",
                hintText: "同一分钟的多条提醒会合并到一个弹窗；如果错过当天时间点，不会补发。",
                useScheduledTime: true,
                defaultScheduledTime: settings.ScheduledReminderTime)
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
        FrameworkElement? extraSettingsContent = null,
        bool useScheduledTime = false,
        string defaultScheduledTime = "09:00")
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
            Foreground = TextSecondaryBrush,
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
            Foreground = TextPrimaryBrush,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var statusText = new TextBlock
        {
            Text = isEnabled ? "已启用" : "已禁用",
            FontSize = 13,
            Foreground = TextSecondaryBrush,
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
            Foreground = TextPrimaryBrush,
            Margin = new Thickness(0, 2, 0, 8)
        };
        container.Children.Add(sectionTitle);

        var inputPanel = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        if (inlineInputPrefixContent != null)
        {
            inputPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        if (useScheduledTime)
        {
            inputPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        inputPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inputPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var timeInputBox = new TextBox
        {
            Width = 84,
            Text = defaultScheduledTime,
            MaxLength = 5,
            Style = Application.Current.Resources["ModernTextBoxStyle"] as Style,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = useScheduledTime ? Visibility.Visible : Visibility.Collapsed
        };

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

        var inputTextColumn = (inlineInputPrefixContent != null ? 1 : 0) + (useScheduledTime ? 1 : 0);
        var addButtonColumn = inputTextColumn + 1;

        if (inlineInputPrefixContent != null)
        {
            inlineInputPrefixContent.Margin = new Thickness(0, 0, 10, 0);
            Grid.SetColumn(inlineInputPrefixContent, 0);
            inputPanel.Children.Add(inlineInputPrefixContent);
        }

        if (useScheduledTime)
        {
            Grid.SetColumn(timeInputBox, inlineInputPrefixContent != null ? 1 : 0);
            inputPanel.Children.Add(timeInputBox);
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
            Foreground = TextMutedBrush,
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
                    Foreground = TextSecondaryBrush,
                    Margin = new Thickness(0, 6, 0, 0)
                });
                return;
            }

            foreach (var item in reminderItems)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                if (useScheduledTime)
                {
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                }

                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var enabledBox = new CheckBox
                {
                    IsChecked = item.IsEnabled,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 12, 0)
                };

                var rowTimeBox = new TextBox
                {
                    Width = 84,
                    Text = string.IsNullOrWhiteSpace(item.ScheduledTime) ? defaultScheduledTime : item.ScheduledTime,
                    MaxLength = 5,
                    Style = Application.Current.Resources["ModernTextBoxStyle"] as Style,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Visibility = useScheduledTime ? Visibility.Visible : Visibility.Collapsed
                };

                var textBlock = new TextBlock
                {
                    Text = item.Text,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = TextPrimaryBrush,
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

                void CommitRowTime()
                {
                    if (!useScheduledTime)
                    {
                        return;
                    }

                    var input = rowTimeBox.Text.Trim();
                    if (!StartupReminderService.TryParseScheduledReminderTime(input, out var parsedTime))
                    {
                        rowTimeBox.Text = string.IsNullOrWhiteSpace(item.ScheduledTime) ? defaultScheduledTime : item.ScheduledTime;
                        hintTextBlock.Text = "请输入有效时间，例如 09:00。";
                        hintTextBlock.Foreground = DangerBrush;
                        return;
                    }

                    item.ScheduledTime = parsedTime.ToString("HH:mm");
                    rowTimeBox.Text = item.ScheduledTime;
                    hintTextBlock.Text = hintText;
                    hintTextBlock.Foreground = TextMutedBrush;
                    SaveReminderSettings();
                }

                rowTimeBox.LostFocus += (s, e) => CommitRowTime();
                rowTimeBox.KeyDown += (s, e) =>
                {
                    if (e.Key == Key.Enter)
                    {
                        CommitRowTime();
                        e.Handled = true;
                    }
                };

                deleteButton.Click += (s, e) =>
                {
                    reminderItems.Remove(item);
                    SaveReminderSettings();
                    RefreshReminderList();
                };

                Grid.SetColumn(enabledBox, 0);
                var textColumn = useScheduledTime ? 2 : 1;
                var deleteColumn = useScheduledTime ? 3 : 2;
                Grid.SetColumn(rowTimeBox, 1);
                Grid.SetColumn(textBlock, textColumn);
                Grid.SetColumn(deleteButton, deleteColumn);
                row.Children.Add(enabledBox);
                if (useScheduledTime)
                {
                    row.Children.Add(rowTimeBox);
                }

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

            var scheduledTime = defaultScheduledTime;
            if (useScheduledTime)
            {
                var timeInput = timeInputBox.Text.Trim();
                if (!StartupReminderService.TryParseScheduledReminderTime(timeInput, out var parsedTime))
                {
                    hintTextBlock.Text = "请输入有效时间，例如 09:00。";
                    hintTextBlock.Foreground = DangerBrush;
                    return;
                }

                scheduledTime = parsedTime.ToString("HH:mm");
                timeInputBox.Text = scheduledTime;
            }

            reminderItems.Add(new StartupReminderEntry
            {
                Text = text,
                IsEnabled = true,
                ScheduledTime = useScheduledTime ? scheduledTime : string.Empty
            });

            inputTextBox.Clear();
            hintTextBlock.Text = hintText;
            hintTextBlock.Foreground = TextMutedBrush;
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
            Foreground = TextPrimaryBrush,
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
            Foreground = TextSecondaryBrush,
            Margin = new Thickness(8, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var timeHintText = new TextBlock
        {
            FontSize = 12,
            Foreground = TextSecondaryBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };

        void ShowTimeHint(string text, Brush? foreground = null)
        {
            timeHintText.Text = text;
            timeHintText.Foreground = foreground ?? TextSecondaryBrush;
        }

        void CommitScheduledTime()
        {
            var input = timeInputBox.Text.Trim();
            if (!StartupReminderService.TryParseScheduledReminderTime(input, out var parsedTime))
            {
                ShowTimeHint("请输入有效时间，例如 09:00。", DangerBrush);
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
            Foreground = TextMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 0)
        });

        return (inputContainer, helperContainer);
    }

    private static FrameworkElement CreateHotKeySettingContentCore()
    {
        var settings = SettingsService.Instance.Settings;
        var container = new StackPanel { Margin = new Thickness(20) };

        var titleText = new TextBlock
        {
            Text = "全局快捷键",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextPrimaryBrush,
            Margin = new Thickness(0, 0, 0, 20)
        };
        container.Children.Add(titleText);

        var descriptionText = new TextBlock
        {
            Text = "设置快速添加待办事项和显示主页的全局快捷键。",
            FontSize = 13,
            Foreground = TextSecondaryBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20)
        };
        container.Children.Add(descriptionText);

        container.Children.Add(CreateHotKeyEditorSection(
            "快速添加待办",
            "按下快捷键时会显示快速添加窗口。",
            null,
            null,
            () => (settings.HotKeyModifiers, settings.HotKeyKey),
            (modifiers, key) =>
            {
                settings.HotKeyModifiers = modifiers;
                settings.HotKeyKey = key;
                SettingsService.Instance.SaveSettings();
            },
            Services.GlobalHotKeyService.MOD_CONTROL | Services.GlobalHotKeyService.MOD_SHIFT | Services.GlobalHotKeyService.MOD_ALT,
            Services.GlobalHotKeyService.VK_Z));

        container.Children.Add(CreateHotKeyEditorSection(
            "显示主页",
            "按下快捷键时会从小组件、托盘或最小化状态回到主页面。",
            settings.ShowHomeHotKeyEnabled,
            isEnabled =>
            {
                settings.ShowHomeHotKeyEnabled = isEnabled;
                SettingsService.Instance.SaveSettings();
            },
            () => (settings.ShowHomeHotKeyModifiers, settings.ShowHomeHotKeyKey),
            (modifiers, key) =>
            {
                settings.ShowHomeHotKeyModifiers = modifiers;
                settings.ShowHomeHotKeyKey = key;
                SettingsService.Instance.SaveSettings();
            },
            Services.GlobalHotKeyService.MOD_CONTROL | Services.GlobalHotKeyService.MOD_SHIFT | Services.GlobalHotKeyService.MOD_ALT,
            0x48));

        return container;
    }

    private static FrameworkElement CreateHotKeyEditorSection(
        string title,
        string description,
        bool? isEnabled,
        Action<bool>? onEnabledChanged,
        Func<(uint Modifiers, uint Key)> getHotKey,
        Action<uint, uint> saveHotKey,
        uint defaultModifiers,
        uint defaultKey)
    {
        var section = new StackPanel { Margin = new Thickness(0, 0, 0, 24) };

        section.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = TextPrimaryBrush,
            Margin = new Thickness(0, 0, 0, 6)
        });

        section.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 12,
            Foreground = TextSecondaryBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        });

        var controlsEnabled = isEnabled != false;
        var hotKey = getHotKey();
        var hotkeyValueText = new TextBlock
        {
            Text = Services.GlobalHotKeyService.GetHotKeyDisplayText(hotKey.Modifiers, hotKey.Key),
            FontSize = 14,
            Foreground = PrimaryBrush,
            FontWeight = FontWeights.Medium,
            VerticalAlignment = VerticalAlignment.Center
        };

        var inputTextBox = new TextBox
        {
            Style = Application.Current.Resources["ModernTextBoxStyle"] as Style,
            Width = 200,
            IsReadOnly = true,
            Text = "点击此处设置快捷键",
            Foreground = TextSecondaryBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = System.Windows.Input.Cursors.Hand
        };

        var resetButton = new Button
        {
            Content = "重置",
            Style = Application.Current.Resources["SecondaryButtonStyle"] as Style,
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var statusText = new TextBlock
        {
            Text = controlsEnabled ? "点击输入框设置新快捷键" : "已禁用",
            FontSize = 13,
            Foreground = TextSecondaryBrush,
            Margin = new Thickness(0, 10, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        void UpdateControlsEnabled(bool enabled)
        {
            controlsEnabled = enabled;
            inputTextBox.IsEnabled = enabled;
            resetButton.IsEnabled = enabled;
            hotkeyValueText.Opacity = enabled ? 1.0 : 0.55;
            statusText.Text = enabled ? "点击输入框设置新快捷键" : "已禁用";
        }

        if (isEnabled.HasValue && onEnabledChanged != null)
        {
            var togglePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var toggleSwitch = new CheckBox
            {
                IsChecked = isEnabled.Value,
                FontSize = 14,
                Foreground = TextPrimaryBrush,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            var toggleStatusText = new TextBlock
            {
                Text = isEnabled.Value ? "已启用" : "已禁用",
                FontSize = 13,
                Foreground = TextSecondaryBrush,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            toggleSwitch.Checked += (s, e) =>
            {
                toggleStatusText.Text = "已启用";
                UpdateControlsEnabled(true);
                onEnabledChanged(true);
            };

            toggleSwitch.Unchecked += (s, e) =>
            {
                toggleStatusText.Text = "已禁用";
                UpdateControlsEnabled(false);
                onEnabledChanged(false);
            };

            togglePanel.Children.Add(toggleSwitch);
            togglePanel.Children.Add(toggleStatusText);
            section.Children.Add(togglePanel);
        }

        var hotkeyPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 12)
        };

        hotkeyPanel.Children.Add(new TextBlock
        {
            Text = "当前快捷键：",
            FontSize = 14,
            Foreground = TextPrimaryBrush,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        hotkeyPanel.Children.Add(hotkeyValueText);
        section.Children.Add(hotkeyPanel);

        var inputPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        };
        inputPanel.Children.Add(inputTextBox);
        inputPanel.Children.Add(resetButton);
        section.Children.Add(inputPanel);
        section.Children.Add(statusText);

        bool isRecording = false;

        inputTextBox.PreviewMouseLeftButtonDown += (s, e) =>
        {
            if (controlsEnabled && !isRecording)
            {
                isRecording = true;
                inputTextBox.Text = "按下快捷键组合...";
                inputTextBox.Foreground = PrimaryBrush;
                statusText.Text = "请按下想要的快捷键组合（至少包含一个修饰键），按 Esc 取消";
                inputTextBox.Focus();
            }
        };

        inputTextBox.PreviewKeyDown += (s, e) =>
        {
            if (!isRecording)
            {
                return;
            }

            e.Handled = true;
            if (e.Key == Key.Escape)
            {
                isRecording = false;
                inputTextBox.Text = "点击此处重新设置快捷键";
                inputTextBox.Foreground = TextSecondaryBrush;
                statusText.Text = "已取消，点击输入框可重新设置";
                return;
            }

            var modifiers = GetCurrentHotKeyModifiers();
            if (modifiers != 0 && TryGetVirtualKey(e.Key, out var key))
            {
                isRecording = false;
                saveHotKey(modifiers, key);
                hotkeyValueText.Text = Services.GlobalHotKeyService.GetHotKeyDisplayText(modifiers, key);
                inputTextBox.Text = "点击此处重新设置快捷键";
                inputTextBox.Foreground = TextSecondaryBrush;
                statusText.Text = "快捷键已更新，点击输入框可再次修改";
                return;
            }

            if (modifiers != 0)
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
                inputTextBox.Foreground = TextSecondaryBrush;
                statusText.Text = "已取消，点击输入框可重新设置";
            }
        };

        resetButton.Click += (s, e) =>
        {
            saveHotKey(defaultModifiers, defaultKey);
            hotkeyValueText.Text = Services.GlobalHotKeyService.GetHotKeyDisplayText(defaultModifiers, defaultKey);
            inputTextBox.Text = "点击此处重新设置快捷键";
            inputTextBox.Foreground = TextSecondaryBrush;
            statusText.Text = "快捷键已重置为默认值，点击输入框可修改";
        };

        UpdateControlsEnabled(controlsEnabled);
        return section;
    }

    private static uint GetCurrentHotKeyModifiers()
    {
        uint modifiers = 0;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers |= Services.GlobalHotKeyService.MOD_CONTROL;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers |= Services.GlobalHotKeyService.MOD_SHIFT;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers |= Services.GlobalHotKeyService.MOD_ALT;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) modifiers |= Services.GlobalHotKeyService.MOD_WIN;
        return modifiers;
    }

    private static bool TryGetVirtualKey(Key keyInput, out uint key)
    {
        key = keyInput switch
        {
            >= Key.A and <= Key.Z => (uint)(0x41 + (keyInput - Key.A)),
            >= Key.D0 and <= Key.D9 => (uint)(0x30 + (keyInput - Key.D0)),
            >= Key.NumPad0 and <= Key.NumPad9 => (uint)(0x60 + (keyInput - Key.NumPad0)),
            >= Key.F1 and <= Key.F24 => (uint)(0x70 + (keyInput - Key.F1)),
            Key.Space => Services.GlobalHotKeyService.VK_SPACE,
            Key.Back => Services.GlobalHotKeyService.VK_BACK,
            Key.Tab => Services.GlobalHotKeyService.VK_TAB,
            Key.Return => Services.GlobalHotKeyService.VK_RETURN,
            Key.Home => Services.GlobalHotKeyService.VK_HOME,
            Key.End => Services.GlobalHotKeyService.VK_END,
            Key.Left => Services.GlobalHotKeyService.VK_LEFT,
            Key.Up => Services.GlobalHotKeyService.VK_UP,
            Key.Right => Services.GlobalHotKeyService.VK_RIGHT,
            Key.Down => Services.GlobalHotKeyService.VK_DOWN,
            Key.Insert => Services.GlobalHotKeyService.VK_INSERT,
            Key.Delete => Services.GlobalHotKeyService.VK_DELETE,
            Key.PageUp => Services.GlobalHotKeyService.VK_PRIOR,
            Key.PageDown => Services.GlobalHotKeyService.VK_NEXT,
            _ => 0
        };

        return key != 0;
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
            Foreground = TextPrimaryBrush,
            Margin = new Thickness(0, 0, 0, 20)
        };
        Grid.SetRow(titleText, 0);
        container.Children.Add(titleText);

        var descriptionText = new TextBlock
        {
            Text = "启用后，程序启动时将自动进入小组件模式，不显示主页面。",
            FontSize = 13,
            Foreground = TextSecondaryBrush,
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
            Foreground = TextPrimaryBrush,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var statusText = new TextBlock
        {
            Name = "StartInWidgetModeStatusText",
            Text = SettingsService.Instance.Settings.StartInWidgetMode ? "小组件模式" : "主页面模式",
            FontSize = 13,
            Foreground = TextSecondaryBrush,
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


