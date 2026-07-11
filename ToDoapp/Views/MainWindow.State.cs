using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.Views;

public partial class MainWindow
{
    private void InitializeData()
    {
        _viewModel.InitializeData();

        if (WidgetView != null)
        {
            WidgetView.TaskChecked += WidgetView_TaskChecked;
            WidgetView.TaskDeleted += WidgetView_TaskDeleted;
            WidgetView.WidgetMouseLeftButtonDown += WidgetView_WidgetMouseLeftButtonDown;
        }

        BindTaskCollections();
        UpdateWidgetTasks();
    }

    private List<TodoItem> GetPendingTodoItems()
    {
        return _viewModel.GetPendingTodoItems();
    }

    private void RefreshTaskCollections()
    {
        _viewModel.RefreshTaskCollections();
        BindTaskCollections();
        UpdateWidgetTasks();
    }

    private void BindTaskCollections()
    {
        if (TasksListBox != null && TasksListBox.ItemsSource != _pendingTasks)
        {
            TasksListBox.ItemsSource = _pendingTasks;
        }

        if (CompletedTasksListBox != null && CompletedTasksListBox.ItemsSource != _completedTasks)
        {
            CompletedTasksListBox.ItemsSource = _completedTasks;
        }

        if (DeletedTasksListBox != null && DeletedTasksListBox.ItemsSource != _deletedTasks)
        {
            DeletedTasksListBox.ItemsSource = _deletedTasks;
        }

        if (ArchivedGroupsListBox != null && ArchivedGroupsListBox.ItemsSource != _archivedGroups)
        {
            ArchivedGroupsListBox.ItemsSource = _archivedGroups;
        }
    }

    private void UpdateWidgetTasks()
    {
        var pendingItems = GetPendingTodoItems();
        WidgetView?.SetTasks(pendingItems);

        if (_widgetWindow != null && _widgetWindow.IsVisible)
        {
            _widgetWindow.SetTasks(pendingItems);
        }
    }

    private void SaveData()
    {
        _viewModel.SaveData();
    }

    private void ReplaceTodoItems(IEnumerable<TodoItem> items)
    {
        _viewModel.ReplaceTodoItems(items);
        BindTaskCollections();
        UpdateWidgetTasks();
    }

    private void QueueStartupPersistenceStatus(string message, string? detailMessage)
    {
        _viewModel.QueueStartupPersistenceStatus(message, detailMessage);
    }

    private void ShowStartupPersistenceNotificationIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(_startupPersistenceMessage))
        {
            return;
        }

        _systemTrayService?.ShowNotification("待办便签", _startupPersistenceMessage);
    }

    private void UpdateStatus(string message, string? detailMessage = null)
    {
        _viewModel.UpdateStatus(message, detailMessage);

        if (StatusTextBlock != null)
        {
            StatusTextBlock.Text = _viewModel.StatusMessage;
            StatusTextBlock.ToolTip = _viewModel.StatusDetail;
        }

        _statusResetTimer.Stop();
        _statusResetTimer.Start();
    }

    private void StatusResetTimer_Tick(object? sender, EventArgs e)
    {
        _statusResetTimer.Stop();
        _viewModel.ResetStatus();
        if (StatusTextBlock != null)
        {
            StatusTextBlock.Text = _viewModel.StatusMessage;
            StatusTextBlock.ToolTip = _viewModel.StatusDetail;
        }
    }

    private void OnSettingsSaveFailed(object? sender, SettingsSaveFailedEventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => OnSettingsSaveFailed(sender, e));
            return;
        }

        UpdateStatus("设置保存失败", e.ErrorMessage);
        _systemTrayService?.ShowNotification("待办便签", "设置保存失败，请检查磁盘空间或文件权限。");
    }

    private void OnHolidayWarmupStatusChanged(object? sender, HolidayWarmupStatusChangedEventArgs e)
    {
        Dispatcher.Invoke(() => UpdateStatus(e.ShortMessage, e.DetailMessage));
    }

    private void UpdateTaskCount()
    {
        _viewModel.UpdateTaskCount();

        if (TaskCountTextBlock != null)
        {
            TaskCountTextBlock.Text = _viewModel.TaskCountDisplay;
        }
    }
}
