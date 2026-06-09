using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.Views;

public partial class MainWindow
{
    private void InitializeTimer()
    {
        _mainTimer.Interval = TimeSpan.FromSeconds(30);
        _mainTimer.Tick += MainTimer_Tick;
        _mainTimer.Start();

        CheckOverdueTasks();
        CleanupExpiredTrashItems();
        CheckScheduledReminder();
    }

    private void MainTimer_Tick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;

        if (now.Date != _lastTimeSensitiveRefreshDate)
        {
            RefreshTimeSensitiveTaskProperties();
            _lastTimeSensitiveRefreshDate = now.Date;
        }

        if (_canPersistData && (now - _lastAutoSaveTime).TotalMinutes >= 5)
        {
            SaveData();
            _lastAutoSaveTime = now;
        }

        if ((now - _lastOverdueCheckTime).TotalHours >= 1)
        {
            CheckOverdueTasks();
            _lastOverdueCheckTime = now;
        }

        if ((now - _lastTrashCleanupTime).TotalHours >= 1)
        {
            CleanupExpiredTrashItems();
            _lastTrashCleanupTime = now;
        }

        if ((now - _lastAutoArchiveCheckTime).TotalHours >= 1)
        {
            CheckAndAutoArchiveCompletedTasks();
            _lastAutoArchiveCheckTime = now;
        }

        CheckScheduledReminder();
    }

    private void RefreshTimeSensitiveTaskProperties()
    {
        _viewModel.RefreshTimeSensitiveTaskProperties();
    }

    private void CleanupExpiredTrashItems()
    {
        var removedCount = _viewModel.CleanupExpiredTrashItems(DateTime.Now);
        if (removedCount == 0)
        {
            return;
        }

        RefreshTaskCollections();
        UpdateStatus(_viewModel.StatusMessage, _viewModel.StatusDetail);
    }

    private void CheckAndAutoArchiveCompletedTasks()
    {
        var autoArchiveDays = SettingsService.Instance.Settings.AutoArchiveDays;
        var archivedCount = _viewModel.CheckAndAutoArchiveCompletedTasks(autoArchiveDays);
        if (archivedCount == 0)
        {
            return;
        }

        RefreshTaskCollections();
        UpdateStatus(_viewModel.StatusMessage, _viewModel.StatusDetail);
    }

    private void CheckOverdueTasks()
    {
        var overdueTasks = _viewModel.GetOverdueTasks();
        if (!overdueTasks.Any())
        {
            return;
        }

        var taskCount = overdueTasks.Count;
        var message = taskCount == 1
            ? $"有 1 个任务已过期：{overdueTasks.First().Title}"
            : $"有 {taskCount} 个任务已过期";

        UpdateStatus(message);
        _systemTrayService?.ShowNotification("待办事项提醒", message);
    }

    private void CheckScheduledReminder()
    {
        var settings = SettingsService.Instance.Settings;
        var now = DateTime.Now;
        var dueReminderEntries = StartupReminderService.GetDueScheduledReminderEntries(settings, now);
        if (dueReminderEntries.Count == 0)
        {
            return;
        }

        var snapshot = new StartupReminderService(_todoService, () => settings).CreateScheduledSnapshot(now);
        if (!snapshot.HasContent)
        {
            return;
        }

        StartupReminderService.MarkScheduledRemindersShown(dueReminderEntries, now);
        SettingsService.Instance.SaveSettings();

        var reminderWindow = new StartupReminderWindow(this, snapshot);
        reminderWindow.ShowDialog();
    }

    private void InitializeSystemTray()
    {
        _systemTrayService = new SystemTrayService(this);
    }

    public void ImportTodosFromJsonFile()
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "导入待办事项",
                Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                DefaultExt = ".json",
                CheckFileExists = true,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog(this) != true)
            {
                return;
            }

            var importedItems = _todoService.LoadTodosFromFile(openFileDialog.FileName);
            if (importedItems.Count == 0)
            {
                const string emptyMessage = "导入文件中没有待办事项";
                UpdateStatus(emptyMessage);
                _systemTrayService?.ShowNotification("待办便签", emptyMessage);
                return;
            }

            var mergeResult = _todoService.MergeImportedTodos(_todoItems, importedItems);
            if (mergeResult.AddedCount == 0)
            {
                const string duplicateMessage = "导入完成，所有待办事项均已存在";
                UpdateStatus(duplicateMessage);
                _systemTrayService?.ShowNotification("待办便签", duplicateMessage);
                return;
            }

            ReplaceTodoItems(mergeResult.MergedTodos);
            SaveData();

            var message = mergeResult.SkippedCount > 0
                ? $"已导入 {mergeResult.AddedCount} 个待办事项，跳过 {mergeResult.SkippedCount} 个重复项"
                : $"已导入 {mergeResult.AddedCount} 个待办事项";
            UpdateStatus(message);
            _systemTrayService?.ShowNotification("待办便签", message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"导入待办事项失败: {ex.Message}");
            var message = $"导入失败：{ex.Message}";
            UpdateStatus(message);
            _systemTrayService?.ShowNotification("待办便签", message);
        }
    }

    public void ExportTodosToJsonFile()
    {
        try
        {
            var exportItems = _todoItems.Where(t => !t.IsDeleted).ToList();

            var saveFileDialog = new SaveFileDialog
            {
                Title = "导出待办事项",
                Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                DefaultExt = ".json",
                AddExtension = true,
                FileName = $"todos-{DateTime.Now:yyyyMMdd-HHmmss}.json",
                OverwritePrompt = true
            };

            if (saveFileDialog.ShowDialog(this) != true)
            {
                return;
            }

            _todoService.ExportTodosToFile(exportItems, saveFileDialog.FileName);

            var message = $"已导出 {exportItems.Count} 个待办事项";
            UpdateStatus(message);
            _systemTrayService?.ShowNotification("待办便签", message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"导出待办事项失败: {ex.Message}");
            var message = $"导出失败：{ex.Message}";
            UpdateStatus(message);
            _systemTrayService?.ShowNotification("待办便签", message);
        }
    }

    public void ShowBackupRecoveryDialog()
    {
        var backupInfos = _todoService.GetBackupInfos();

        var rootPanel = new StackPanel();
        rootPanel.Children.Add(new TextBlock
        {
            Text = "选择一个备份恢复点。恢复后将立即刷新当前待办列表。",
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.Resources["DialogSecondaryForegroundBrush"],
            Margin = new Thickness(0, 0, 0, 12)
        });

        var emptyStateText = new TextBlock
        {
            Text = "暂无可用备份。",
            Foreground = (Brush)Application.Current.Resources["DialogSecondaryForegroundBrush"],
            Margin = new Thickness(0, 0, 0, 12),
            Visibility = backupInfos.Count == 0 ? Visibility.Visible : Visibility.Collapsed
        };
        rootPanel.Children.Add(emptyStateText);

        var backupListBox = new ListBox
        {
            Height = 260,
            ItemsSource = backupInfos,
            Visibility = backupInfos.Count > 0 ? Visibility.Visible : Visibility.Collapsed,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            ItemContainerStyle = (Style)Application.Current.Resources["DialogBackupListBoxItemStyle"],
            ItemTemplate = (DataTemplate)Application.Current.Resources["BackupItemDataTemplate"]
        };
        backupListBox.SelectedIndex = backupInfos.Count > 0 ? 0 : -1;
        rootPanel.Children.Add(backupListBox);

        var statusTextBlock = new TextBlock
        {
            Foreground = (Brush)Application.Current.Resources["DangerBrush"],
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0),
            Visibility = Visibility.Collapsed
        };
        rootPanel.Children.Add(statusTextBlock);

        DialogService.OnDialogConfirmed = _ =>
        {
            if (backupListBox.SelectedItem is not TodoBackupInfo selectedBackup)
            {
                statusTextBlock.Text = "请选择一个备份文件。";
                statusTextBlock.Visibility = Visibility.Visible;
                return false;
            }

            var restoreResult = _todoService.RestoreFromBackup(selectedBackup.FilePath);
            if (!restoreResult.IsSuccess)
            {
                statusTextBlock.Text = restoreResult.ErrorMessage ?? "恢复失败，请稍后重试。";
                statusTextBlock.Visibility = Visibility.Visible;
                return false;
            }

            _canPersistData = true;
            _startupPersistenceMessage = null;
            _startupPersistenceDetail = null;
            ReplaceTodoItems(restoreResult.Todos);

            var restoredBackup = restoreResult.BackupInfo ?? selectedBackup;
            var message = $"已恢复备份：{restoredBackup.BackupTimeDisplay}";
            UpdateStatus(message);
            _systemTrayService?.ShowNotification("待办便签", message);
            return true;
        };

        try
        {
            DialogService.ShowCustomDialog(
                "恢复备份",
                DialogType.None,
                rootPanel,
                "恢复",
                "取消",
                (primaryButton, _) =>
                {
                    primaryButton.IsEnabled = backupInfos.Count > 0 && backupListBox.SelectedItem != null;
                    backupListBox.SelectionChanged += (_, _) =>
                    {
                        primaryButton.IsEnabled = backupListBox.SelectedItem != null;
                        statusTextBlock.Visibility = Visibility.Collapsed;
                    };
                },
                false,
                420);
        }
        finally
        {
            DialogService.OnDialogConfirmed = null;
        }
    }

    private void InitializeGlobalHotKey()
    {
        try
        {
            _globalHotKeyService = new GlobalHotKeyService(this);
            _globalHotKeyService.HotKeyPressed += OnGlobalHotKeyPressed;

            RegisterConfiguredHotKeys("已注册");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"初始化全局快捷键失败: {ex.Message}");
            UpdateStatus("全局快捷键初始化失败");
        }
    }

    private void RegisterConfiguredHotKeys(string statusVerb)
    {
        if (_globalHotKeyService == null)
        {
            return;
        }

        var settings = SettingsService.Instance.Settings;

        var quickAddHotKeyId = -1;
        if (settings.QuickAddHotKeyEnabled)
        {
            quickAddHotKeyId = _globalHotKeyService.RegisterHotKey(
                GlobalHotKeyAction.QuickAdd,
                settings.HotKeyModifiers,
                settings.HotKeyKey);
        }
        else
        {
            _globalHotKeyService.UnregisterHotKey(GlobalHotKeyAction.QuickAdd);
        }

        var showHomeHotKeyId = -1;
        if (settings.ShowHomeHotKeyEnabled)
        {
            showHomeHotKeyId = _globalHotKeyService.RegisterHotKey(
                GlobalHotKeyAction.ShowHome,
                settings.ShowHomeHotKeyModifiers,
                settings.ShowHomeHotKeyKey);
        }
        else
        {
            _globalHotKeyService.UnregisterHotKey(GlobalHotKeyAction.ShowHome);
        }

        var hideWidgetHotKeyId = -1;
        if (settings.HideWidgetHotKeyEnabled)
        {
            hideWidgetHotKeyId = _globalHotKeyService.RegisterHotKey(
                GlobalHotKeyAction.HideWidget,
                settings.HideWidgetHotKeyModifiers,
                settings.HideWidgetHotKeyKey);
        }
        else
        {
            _globalHotKeyService.UnregisterHotKey(GlobalHotKeyAction.HideWidget);
        }

        var toggleWidgetModeHotKeyId = -1;
        if (settings.ToggleWidgetModeHotKeyEnabled)
        {
            toggleWidgetModeHotKeyId = _globalHotKeyService.RegisterHotKey(
                GlobalHotKeyAction.ToggleWidgetMode,
                settings.ToggleWidgetModeHotKeyModifiers,
                settings.ToggleWidgetModeHotKeyKey);
        }
        else
        {
            _globalHotKeyService.UnregisterHotKey(GlobalHotKeyAction.ToggleWidgetMode);
        }

        var quickAddText = settings.QuickAddHotKeyEnabled
            ? GlobalHotKeyService.GetHotKeyDisplayText(settings.HotKeyModifiers, settings.HotKeyKey)
            : "未启用";
        var showHomeText = settings.ShowHomeHotKeyEnabled
            ? GlobalHotKeyService.GetHotKeyDisplayText(settings.ShowHomeHotKeyModifiers, settings.ShowHomeHotKeyKey)
            : "未启用";
        var hideWidgetText = settings.HideWidgetHotKeyEnabled
            ? GlobalHotKeyService.GetHotKeyDisplayText(settings.HideWidgetHotKeyModifiers, settings.HideWidgetHotKeyKey)
            : "未启用";
        var toggleWidgetModeText = settings.ToggleWidgetModeHotKeyEnabled
            ? GlobalHotKeyService.GetHotKeyDisplayText(settings.ToggleWidgetModeHotKeyModifiers, settings.ToggleWidgetModeHotKeyKey)
            : "未启用";

        var allRegistered = quickAddHotKeyId != -1
            && (!settings.ShowHomeHotKeyEnabled || showHomeHotKeyId != -1)
            && (!settings.HideWidgetHotKeyEnabled || hideWidgetHotKeyId != -1)
            && (!settings.ToggleWidgetModeHotKeyEnabled || toggleWidgetModeHotKeyId != -1);

        if (allRegistered)
        {
            UpdateStatus($"全局快捷键{statusVerb}：快速添加 {quickAddText}；显示主页 {showHomeText}；隐藏小组件 {hideWidgetText}；切到小组件模式 {toggleWidgetModeText}");
            return;
        }

        UpdateStatus($"全局快捷键{statusVerb}不完整：快速添加 {quickAddText}；显示主页 {showHomeText}；隐藏小组件 {hideWidgetText}；切到小组件模式 {toggleWidgetModeText}");
    }

    private void OnGlobalHotKeyPressed(GlobalHotKeyAction action)
    {
        switch (action)
        {
            case GlobalHotKeyAction.QuickAdd:
                OnQuickAddHotKeyPressed();
                break;
            case GlobalHotKeyAction.ShowHome:
                OnShowHomeHotKeyPressed();
                break;
            case GlobalHotKeyAction.HideWidget:
                OnHideWidgetHotKeyPressed();
                break;
            case GlobalHotKeyAction.ToggleWidgetMode:
                OnToggleWidgetModeHotKeyPressed();
                break;
        }
    }

    private void OnQuickAddHotKeyPressed()
    {
        try
        {
            Dispatcher.Invoke(() =>
            {
                if (_quickAddWindow != null && _quickAddWindow.IsVisible)
                {
                    _quickAddWindow.Activate();
                    _quickAddWindow.FocusInput();
                    return;
                }

                if (!_canPersistData)
                {
                    UpdateStatus("待办数据加载失败，暂不可使用快速添加", _startupPersistenceDetail);
                    return;
                }

                _quickAddWindow = new QuickAddWindow(_todoService, _todoItems)
                {
                    Owner = this
                };
                _quickAddWindow.Closed += (_, _) => _quickAddWindow = null;
                var result = _quickAddWindow.ShowDialog();
                if (result == true)
                {
                    RefreshTaskCollections();
                    UpdateTaskCount();
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"处理全局快捷键失败: {ex.Message}");
            UpdateStatus("打开快速添加窗口失败");
        }
    }

    private void OnShowHomeHotKeyPressed()
    {
        try
        {
            Dispatcher.Invoke(RestoreMainWindow);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"处理显示主页快捷键失败: {ex.Message}");
            UpdateStatus("显示主页失败");
        }
    }

    private void OnHideWidgetHotKeyPressed()
    {
        try
        {
            Dispatcher.Invoke(() =>
            {
                if (!IsWidgetMode()) return;
                ToggleWidgetWindowVisibility();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"处理隐藏小组件快捷键失败: {ex.Message}");
            UpdateStatus("隐藏小组件失败");
        }
    }

    private void OnToggleWidgetModeHotKeyPressed()
    {
        try
        {
            Dispatcher.Invoke(EnterWidgetMode);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"处理切到小组件模式快捷键失败: {ex.Message}");
            UpdateStatus("切到小组件模式失败");
        }
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        try
        {
            if (_globalHotKeyService == null || !_isLoaded)
            {
                return;
            }

            RegisterConfiguredHotKeys("已更新");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"更新全局快捷键失败: {ex.Message}");
        }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        SaveData();

        var result = DialogService.ShowConfirm("确定要退出待办便签应用吗？", "确认退出");
        if (result == Services.DialogResult.OK)
        {
            _systemTrayService?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
    }

    public void MinimizeHostWindow()
    {
        Hide();
    }

    public void ExitApplication()
    {
        _systemTrayService?.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
}
