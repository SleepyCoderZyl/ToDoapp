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
    }

    private void MainTimer_Tick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;

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
    }

    private void CleanupExpiredTrashItems()
    {
        var itemsToRemove = _todoItems
            .Where(t => t.IsDeleted && t.DeletedDate.HasValue && (DateTime.Now - t.DeletedDate.Value).Days >= 7)
            .ToList();

        if (!itemsToRemove.Any())
        {
            return;
        }

        foreach (var item in itemsToRemove)
        {
            _todoItems.Remove(item);
        }

        RefreshTaskCollections();
        UpdateTaskCount();
        SaveData();
        UpdateStatus($"已自动清理 {itemsToRemove.Count} 个过期垃圾箱任务");
    }

    private void CheckAndAutoArchiveCompletedTasks()
    {
        var autoArchiveDays = SettingsService.Instance.Settings.AutoArchiveDays;
        var tasksToArchive = _todoItems
            .Where(t =>
                !t.IsDeleted &&
                !t.IsArchived &&
                t.IsCompleted &&
                t.CompletedDate.HasValue &&
                (DateTime.Now - t.CompletedDate.Value).Days >= autoArchiveDays)
            .ToList();

        if (!tasksToArchive.Any())
        {
            return;
        }

        foreach (var task in tasksToArchive)
        {
            task.IsArchived = true;
        }

        RefreshTaskCollections();
        UpdateTaskCount();
        SaveData();
        UpdateStatus($"已自动归档 {tasksToArchive.Count} 个已完成任务");
    }

    private void CheckOverdueTasks()
    {
        var overdueTasks = _todoItems.Where(t => !t.IsDeleted && !t.IsArchived && t.IsOverdue).ToList();
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

            foreach (var item in importedItems)
            {
                _todoItems.Add(item);
            }

            RefreshTaskCollections();
            UpdateTaskCount();
            SaveData();

            var message = $"已导入 {importedItems.Count} 个待办事项";
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

        var backupListView = new ListView
        {
            Height = 280,
            ItemsSource = backupInfos,
            Visibility = backupInfos.Count > 0 ? Visibility.Visible : Visibility.Collapsed,
            Background = (Brush)Application.Current.Resources["SurfaceBrush"],
            BorderBrush = (Brush)Application.Current.Resources["BackgroundLightBrush"],
            BorderThickness = new Thickness(1)
        };

        var gridView = new GridView();
        gridView.Columns.Add(new GridViewColumn
        {
            Header = "备份时间",
            DisplayMemberBinding = new Binding(nameof(TodoBackupInfo.BackupTimeDisplay))
        });
        gridView.Columns.Add(new GridViewColumn
        {
            Header = "文件大小",
            DisplayMemberBinding = new Binding(nameof(TodoBackupInfo.FileSizeDisplay)),
            Width = 80
        });
        backupListView.View = gridView;
        backupListView.SelectedIndex = backupInfos.Count > 0 ? 0 : -1;
        rootPanel.Children.Add(backupListView);

        var statusTextBlock = new TextBlock
        {
            Foreground = Brushes.OrangeRed,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 0),
            Visibility = Visibility.Collapsed
        };
        rootPanel.Children.Add(statusTextBlock);

        DialogService.OnDialogConfirmed = _ =>
        {
            if (backupListView.SelectedItem is not TodoBackupInfo selectedBackup)
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
                    primaryButton.IsEnabled = backupInfos.Count > 0 && backupListView.SelectedItem != null;
                    backupListView.SelectionChanged += (_, _) =>
                    {
                        primaryButton.IsEnabled = backupListView.SelectedItem != null;
                        statusTextBlock.Visibility = Visibility.Collapsed;
                    };
                });
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

            var settings = SettingsService.Instance.Settings;
            var hotKeyId = _globalHotKeyService.RegisterHotKey(settings.HotKeyModifiers, settings.HotKeyKey);

            if (hotKeyId != -1)
            {
                UpdateStatus($"全局快捷键已注册：{_globalHotKeyService.GetHotKeyDisplayText()}");
            }
            else
            {
                UpdateStatus("全局快捷键等待注册...");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"初始化全局快捷键失败: {ex.Message}");
            UpdateStatus("全局快捷键初始化失败");
        }
    }

    private void OnGlobalHotKeyPressed()
    {
        try
        {
            Dispatcher.Invoke(() =>
            {
                if (_quickAddWindow != null && _quickAddWindow.IsVisible)
                {
                    _quickAddWindow.Activate();
                    _quickAddWindow.Focus();
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

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        try
        {
            if (_globalHotKeyService == null || !_isLoaded)
            {
                return;
            }

            var settings = SettingsService.Instance.Settings;
            var hotKeyId = _globalHotKeyService.RegisterHotKey(settings.HotKeyModifiers, settings.HotKeyKey);

            if (hotKeyId != -1)
            {
                UpdateStatus($"全局快捷键已更新：{_globalHotKeyService.GetHotKeyDisplayText()}");
            }
            else
            {
                UpdateStatus("全局快捷键更新失败");
            }
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
}
