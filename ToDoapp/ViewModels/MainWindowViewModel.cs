using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private Dictionary<string, bool> _archivedExpansionStates = new();
    private bool _canPersistData = true;
    private string? _startupPersistenceMessage;
    private string? _startupPersistenceDetail;
    private string _statusMessage = "准备就绪";
    private string _statusDetail = "准备就绪";
    private string _taskCountDisplay = string.Empty;

    public MainWindowViewModel(ITodoService todoService)
    {
        TodoService = todoService;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ITodoService TodoService { get; }

    public ObservableCollection<TodoItem> TodoItems { get; } = new();

    public ObservableCollection<TodoItem> PendingTasks { get; } = new();

    public ObservableCollection<TodoItem> CompletedTasks { get; } = new();

    public ObservableCollection<TodoItem> DeletedTasks { get; } = new();

    public ObservableCollection<TodoItem> ArchivedTasks { get; } = new();

    public ObservableCollection<ArchivedGroup> ArchivedGroups { get; private set; } = new();

    public bool CanPersistData
    {
        get => _canPersistData;
        set
        {
            if (_canPersistData != value)
            {
                _canPersistData = value;
                OnPropertyChanged();
            }
        }
    }

    public string? StartupPersistenceMessage
    {
        get => _startupPersistenceMessage;
        set
        {
            if (_startupPersistenceMessage != value)
            {
                _startupPersistenceMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public string? StartupPersistenceDetail
    {
        get => _startupPersistenceDetail;
        set
        {
            if (_startupPersistenceDetail != value)
            {
                _startupPersistenceDetail = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusDetail
    {
        get => _statusDetail;
        private set
        {
            if (_statusDetail != value)
            {
                _statusDetail = value;
                OnPropertyChanged();
            }
        }
    }

    public string TaskCountDisplay
    {
        get => _taskCountDisplay;
        private set
        {
            if (_taskCountDisplay != value)
            {
                _taskCountDisplay = value;
                OnPropertyChanged();
            }
        }
    }

    public void InitializeData()
    {
        var loadResult = TodoService.LoadTodos();
        CanPersistData = loadResult.IsSuccess;
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
        UpdateTaskCount();
        CheckAndAutoArchiveCompletedTasks(SettingsService.Instance.Settings.AutoArchiveDays);
    }

    public TodoItem? AddSmartTask(string input, DateTime? selectedDueDate)
    {
        if (string.IsNullOrWhiteSpace(input) || input == "添加新的待办事项...")
        {
            UpdateStatus("请输入待办事项内容");
            return null;
        }

        var parsedResult = SmartTodoParser.Parse(input);
        if (string.IsNullOrWhiteSpace(parsedResult.Title))
        {
            UpdateStatus("无法解析待办事项内容");
            return null;
        }

        var todoItem = new TodoItem
        {
            Title = parsedResult.Title,
            CreatedDate = DateTime.Now,
            IsCompleted = false
        };

        if (selectedDueDate.HasValue)
        {
            // 日期选择器不提供时间 → 不自动开启提醒，避免与"必须具体时间"规则冲突
            todoItem.DueDate = selectedDueDate.Value;
        }
        else if (parsedResult.DueDate.HasValue)
        {
            todoItem.DueDate = parsedResult.DueDate.Value;
            todoItem.DueTime = parsedResult.DueTime;
            todoItem.ReminderOffsetMinutes = parsedResult.ReminderOffsetMinutes;
            // 仅当解析得到具体时间时，才标记"开启提醒"
            todoItem.HasReminder = parsedResult.DueTime.HasValue;
        }

        TodoItems.Insert(0, todoItem);
        RefreshTaskCollections();
        UpdateTaskCount();

        var dateInfo = todoItem.DueDate.HasValue
            ? $" (截止: {BuildReminderDateLabel(todoItem)})"
            : string.Empty;
        UpdateStatus($"已添加: {parsedResult.Title}{dateInfo}");
        SaveData();

        return todoItem;
    }

    private static string BuildReminderDateLabel(TodoItem todoItem)
    {
        var dateText = todoItem.DueDate!.Value.ToString("MM-dd");
        if (todoItem.DueTime.HasValue)
        {
            dateText += $" {todoItem.DueTime.Value:HH:mm}";
        }

        if (todoItem.ReminderOffsetMinutes.HasValue && todoItem.ReminderOffsetMinutes.Value > 0)
        {
            dateText += $" (提前{todoItem.ReminderOffsetMinutes.Value}分钟)";
        }

        return dateText;
    }

    public List<TodoItem> GetPendingTodoItems()
    {
        return TodoItems
            .Where(t => !t.IsDeleted && !t.IsArchived && !t.IsCompleted)
            .OrderBy(t => t.DueDate.HasValue ? 0 : 1)
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(t => t.CreatedDate)
            .ToList();
    }

    public void RefreshTaskCollections()
    {
        var pendingItems = GetPendingTodoItems();
        var completedItems = TodoItems
            .Where(t => !t.IsDeleted && !t.IsArchived && t.IsCompleted)
            .OrderByDescending(t => t.CompletedDate)
            .ToList();
        var deletedItems = TodoItems
            .Where(t => t.IsDeleted)
            .OrderByDescending(t => t.DeletedDate)
            .ToList();
        var archivedItems = TodoItems
            .Where(t => t.IsArchived)
            .OrderByDescending(t => t.ArchivedDate)
            .ToList();

        UpdateCollection(PendingTasks, pendingItems);
        UpdateCollection(CompletedTasks, completedItems);
        UpdateCollection(DeletedTasks, deletedItems);
        UpdateCollection(ArchivedTasks, archivedItems);
        UpdateArchivedGroups();
    }

    public void ReplaceTodoItems(IEnumerable<TodoItem> items)
    {
        TodoItems.Clear();
        foreach (var item in items)
        {
            TodoItems.Add(item);
        }

        RefreshTaskCollections();
        UpdateTaskCount();
    }

    public TodoSaveResult SaveData()
    {
        if (!CanPersistData)
        {
            UpdateStatus("待办数据加载失败，已阻止覆盖原文件", StartupPersistenceDetail);
            return TodoSaveResult.Failure(StartupPersistenceDetail);
        }

        var saveResult = TodoService.SaveTodos(TodoItems);
        if (!saveResult.IsSuccess)
        {
            UpdateStatus("保存待办事项失败", saveResult.ErrorMessage);
        }

        return saveResult;
    }

    public void MoveTaskToTrash(TodoItem todoItem)
    {
        todoItem.IsDeleted = true;
        RefreshTaskCollections();
        UpdateTaskCount();
        UpdateStatus($"已移至垃圾箱: {todoItem.Title}");
        SaveData();
    }

    public void RestoreDeletedTask(TodoItem todoItem)
    {
        todoItem.IsDeleted = false;
        RefreshTaskCollections();
        UpdateTaskCount();
        UpdateStatus($"已恢复: {todoItem.Title}");
        SaveData();
    }

    public void PermanentlyDeleteTask(TodoItem todoItem)
    {
        TodoItems.Remove(todoItem);
        RefreshTaskCollections();
        UpdateTaskCount();
        UpdateStatus($"已永久删除: {todoItem.Title}");
        SaveData();
    }

    public void ArchiveTask(TodoItem todoItem)
    {
        todoItem.IsArchived = true;
        RefreshTaskCollections();
        UpdateStatus($"已归档: {todoItem.Title}");
        SaveData();
    }

    public void UnarchiveTask(TodoItem todoItem)
    {
        todoItem.IsArchived = false;
        RefreshTaskCollections();
        UpdateStatus($"已取消归档: {todoItem.Title}");
        SaveData();
    }

    public int EmptyTrash()
    {
        var itemsToRemove = TodoItems.Where(t => t.IsDeleted).ToList();
        foreach (var item in itemsToRemove)
        {
            TodoItems.Remove(item);
        }

        RefreshTaskCollections();
        UpdateTaskCount();
        UpdateStatus("垃圾箱已清空");
        SaveData();
        return itemsToRemove.Count;
    }

    public int UnarchiveAll()
    {
        var itemsToRestore = TodoItems.Where(t => t.IsArchived).ToList();
        foreach (var item in itemsToRestore)
        {
            item.IsArchived = false;
        }

        RefreshTaskCollections();
        UpdateTaskCount();
        UpdateStatus("所有归档任务已恢复");
        SaveData();
        return itemsToRestore.Count;
    }

    public int UnarchiveGroup(ArchivedGroup group)
    {
        var tasksToRestore = group.Tasks.ToList();
        foreach (var task in tasksToRestore)
        {
            task.IsArchived = false;
        }

        RefreshTaskCollections();
        UpdateTaskCount();
        UpdateStatus($"已恢复 {tasksToRestore.Count} 个任务");
        SaveData();
        return tasksToRestore.Count;
    }

    public int CleanupExpiredTrashItems(DateTime now)
    {
        var itemsToRemove = TodoItems
            .Where(t => t.IsDeleted && t.DeletedDate.HasValue && (now - t.DeletedDate.Value).Days >= 7)
            .ToList();

        if (!itemsToRemove.Any())
        {
            return 0;
        }

        foreach (var item in itemsToRemove)
        {
            TodoItems.Remove(item);
        }

        RefreshTaskCollections();
        UpdateTaskCount();
        SaveData();
        UpdateStatus($"已自动清理 {itemsToRemove.Count} 个过期垃圾箱任务");
        return itemsToRemove.Count;
    }

    public int CheckAndAutoArchiveCompletedTasks(int autoArchiveDays)
    {
        var tasksToArchive = TodoItems
            .Where(t =>
                !t.IsDeleted &&
                !t.IsArchived &&
                t.IsCompleted &&
                t.CompletedDate.HasValue &&
                (DateTime.Now - t.CompletedDate.Value).Days >= autoArchiveDays)
            .ToList();

        if (!tasksToArchive.Any())
        {
            return 0;
        }

        foreach (var task in tasksToArchive)
        {
            task.IsArchived = true;
        }

        RefreshTaskCollections();
        UpdateTaskCount();
        SaveData();
        UpdateStatus($"已自动归档 {tasksToArchive.Count} 个已完成任务");
        return tasksToArchive.Count;
    }

    public IReadOnlyList<TodoItem> GetOverdueTasks()
    {
        return TodoItems.Where(t => !t.IsDeleted && !t.IsArchived && t.IsOverdue).ToList();
    }

    public void RefreshTimeSensitiveTaskProperties()
    {
        foreach (var task in TodoItems)
        {
            task.RefreshTimeSensitiveProperties();
        }
    }

    public void UpdateTaskCount()
    {
        var total = TodoItems.Count;
        var completed = TodoItems.Count(x => x.IsCompleted);
        TaskCountDisplay = $"({total}项 • 已完成{completed}项)";
    }

    public void QueueStartupPersistenceStatus(string message, string? detailMessage)
    {
        StartupPersistenceMessage = message;
        StartupPersistenceDetail = detailMessage;
        UpdateStatus(message, detailMessage);
    }

    public void UpdateStatus(string message, string? detailMessage = null)
    {
        StatusMessage = message;
        StatusDetail = string.IsNullOrWhiteSpace(detailMessage) ? message : detailMessage;
    }

    public void ResetStatus()
    {
        UpdateStatus("准备就绪");
    }

    public static string BuildTaskClipboardText(TodoItem todoItem)
    {
        var text = todoItem.Title;
        if (todoItem.DueDate.HasValue)
        {
            text += $" {todoItem.DueDate.Value:yyyy-MM-dd}";
        }

        return text;
    }

    private void UpdateArchivedGroups()
    {
        _archivedExpansionStates = CaptureArchivedExpansionStates();
        ArchivedGroups = ArchivedGroup.BuildGroupTree(ArchivedTasks);
        var hasSavedExpansionState = _archivedExpansionStates.Count > 0;

        if (hasSavedExpansionState)
        {
            ApplyArchivedExpansionStates(ArchivedGroups, null);
        }

        if (!hasSavedExpansionState)
        {
            var currentWeekNumber = GetCurrentWeekNumber();
            foreach (var yearGroup in ArchivedGroups)
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

        _archivedExpansionStates = CaptureArchivedExpansionStates();
        OnPropertyChanged(nameof(ArchivedGroups));
    }

    private Dictionary<string, bool> CaptureArchivedExpansionStates()
    {
        var states = new Dictionary<string, bool>();

        foreach (var group in ArchivedGroups)
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

    private static void UpdateCollection<T>(ObservableCollection<T> collection, IReadOnlyList<T> newItems)
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
