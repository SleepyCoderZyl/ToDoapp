using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.Views;

public partial class MainWindow
{
    private void InitializeData()
    {
        var loadResult = _todoService.LoadTodos();
        _canPersistData = loadResult.IsSuccess;
        ReplaceTodoItems(loadResult.Todos);

        if (!loadResult.IsSuccess)
        {
            QueueStartupPersistenceStatus(
                "待办数据加载失败，已阻止覆盖原文件",
                loadResult.ErrorMessage);
        }
        else if (loadResult.IsRecoveredFromBackup)
        {
            QueueStartupPersistenceStatus(
                "已从最近备份恢复待办数据",
                loadResult.ErrorMessage);
        }

        RefreshTaskCollections();

        if (WidgetView != null)
        {
            WidgetView.TaskChecked += WidgetView_TaskChecked;
            WidgetView.TaskDeleted += WidgetView_TaskDeleted;
            WidgetView.WidgetMouseLeftButtonDown += WidgetView_WidgetMouseLeftButtonDown;
        }

        UpdateTaskCount();
        CheckAndAutoArchiveCompletedTasks();
    }

    private List<TodoItem> GetPendingTodoItems()
    {
        return _todoItems
            .Where(t => !t.IsDeleted && !t.IsArchived && !t.IsCompleted)
            .OrderBy(t => t.DueDate.HasValue ? 0 : 1)
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(t => t.CreatedDate)
            .ToList();
    }

    private void RefreshTaskCollections()
    {
        var pendingItems = GetPendingTodoItems();
        var completedItems = _todoItems
            .Where(t => !t.IsDeleted && !t.IsArchived && t.IsCompleted)
            .OrderByDescending(t => t.CompletedDate)
            .ToList();
        var deletedItems = _todoItems
            .Where(t => t.IsDeleted)
            .OrderByDescending(t => t.DeletedDate)
            .ToList();
        var archivedItems = _todoItems
            .Where(t => t.IsArchived)
            .OrderByDescending(t => t.ArchivedDate)
            .ToList();

        UpdateCollection(_pendingTasks, pendingItems);
        UpdateCollection(_completedTasks, completedItems);
        UpdateCollection(_deletedTasks, deletedItems);
        UpdateCollection(_archivedTasks, archivedItems);

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

        if (ArchivedGroupsListBox != null)
        {
            UpdateArchivedGroups();
        }

        WidgetView?.SetTasks(pendingItems);

        if (_widgetWindow != null && _widgetWindow.IsVisible)
        {
            _widgetWindow.SetTasks(pendingItems);
        }
    }

    private void UpdateCollection<T>(ObservableCollection<T> collection, IReadOnlyList<T> newItems)
        where T : class
    {
        var toRemove = collection.Where(item => !newItems.Contains(item)).ToList();
        foreach (var item in toRemove)
        {
            collection.Remove(item);
        }

        for (int i = 0; i < newItems.Count; i++)
        {
            var item = newItems[i];
            var existingIndex = collection.IndexOf(item);

            if (existingIndex == -1)
            {
                collection.Insert(i, item);
            }
            else if (existingIndex != i)
            {
                collection.Move(existingIndex, i);
            }
        }
    }

    private void UpdateArchivedGroups()
    {
        _archivedExpansionStates = CaptureArchivedExpansionStates();
        _archivedGroups = ArchivedGroup.BuildGroupTree(_archivedTasks);
        var hasSavedExpansionState = _archivedExpansionStates.Count > 0;

        if (hasSavedExpansionState)
        {
            ApplyArchivedExpansionStates(_archivedGroups, null);
        }

        if (!hasSavedExpansionState)
        {
            var currentWeekNumber = GetCurrentWeekNumber();
            foreach (var yearGroup in _archivedGroups)
            {
                foreach (var monthGroup in yearGroup.Children)
                {
                    foreach (var weekGroup in monthGroup.Children)
                    {
                        if (weekGroup.Name == $"第{currentWeekNumber}周")
                        {
                            yearGroup.IsExpanded = true;
                            monthGroup.IsExpanded = true;
                            weekGroup.IsExpanded = true;
                        }
                    }
                }
            }
        }

        if (ArchivedGroupsListBox != null)
        {
            ArchivedGroupsListBox.ItemsSource = _archivedGroups;
        }

        _archivedExpansionStates = CaptureArchivedExpansionStates();
    }

    private Dictionary<string, bool> CaptureArchivedExpansionStates()
    {
        var states = new Dictionary<string, bool>();

        foreach (var group in _archivedGroups)
        {
            CaptureArchivedExpansionStates(group, null, states);
        }

        return states;
    }

    private static void CaptureArchivedExpansionStates(
        ArchivedGroup group,
        string? parentKey,
        Dictionary<string, bool> states)
    {
        var key = BuildArchivedGroupKey(group, parentKey);
        states[key] = group.IsExpanded;

        foreach (var child in group.Children)
        {
            CaptureArchivedExpansionStates(child, key, states);
        }
    }

    private void ApplyArchivedExpansionStates(IEnumerable<ArchivedGroup> groups, string? parentKey)
    {
        foreach (var group in groups)
        {
            var key = BuildArchivedGroupKey(group, parentKey);
            if (_archivedExpansionStates.TryGetValue(key, out var isExpanded))
            {
                group.IsExpanded = isExpanded;
            }

            ApplyArchivedExpansionStates(group.Children, key);
        }
    }

    private static string BuildArchivedGroupKey(ArchivedGroup group, string? parentKey)
    {
        var currentKey = $"{group.Level}:{group.Name}";
        return string.IsNullOrEmpty(parentKey) ? currentKey : $"{parentKey}/{currentKey}";
    }

    private static int GetCurrentWeekNumber()
    {
        return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            DateTime.Now,
            CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);
    }

    private void SaveData()
    {
        if (!_canPersistData)
        {
            UpdateStatus("待办数据加载失败，已阻止覆盖原文件", _startupPersistenceDetail);
            return;
        }

        var saveResult = _todoService.SaveTodos(_todoItems);
        if (!saveResult.IsSuccess)
        {
            UpdateStatus("保存待办事项失败", saveResult.ErrorMessage);
        }
    }

    private void ReplaceTodoItems(IEnumerable<TodoItem> items)
    {
        _todoItems.Clear();
        foreach (var item in items)
        {
            _todoItems.Add(item);
        }

        RefreshTaskCollections();
        UpdateTaskCount();
    }

    private void QueueStartupPersistenceStatus(string message, string? detailMessage)
    {
        _startupPersistenceMessage = message;
        _startupPersistenceDetail = detailMessage;
        UpdateStatus(message, detailMessage);
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
        if (StatusTextBlock != null)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.ToolTip = string.IsNullOrWhiteSpace(detailMessage) ? message : detailMessage;
        }

        Task.Delay(5000).ContinueWith(_ =>
        {
            Dispatcher.Invoke(() =>
            {
                if (StatusTextBlock != null)
                {
                    StatusTextBlock.Text = "准备就绪";
                    StatusTextBlock.ToolTip = "准备就绪";
                }
            });
        });
    }

    private void OnHolidayWarmupStatusChanged(object? sender, HolidayWarmupStatusChangedEventArgs e)
    {
        Dispatcher.Invoke(() => UpdateStatus(e.ShortMessage, e.DetailMessage));
    }

    private void UpdateTaskCount()
    {
        if (TaskCountTextBlock == null)
        {
            return;
        }

        var total = _todoItems.Count;
        var completed = _todoItems.Count(x => x.IsCompleted);
        TaskCountTextBlock.Text = $"({total}项 • 已完成{completed}项)";
    }
}
