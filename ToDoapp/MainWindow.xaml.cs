using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Media.Animation;
using System.Windows.Interop;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using ToDoapp.Models;
using ToDoapp.Services;
using ToDoapp.Views;
using ToDoapp.Widgets;

namespace ToDoapp;

public class WidthAdjustConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double actualWidth && actualWidth > 0)
        {
            return Math.Max(50, actualWidth - 71);
        }
        return 150;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public partial class MainWindow : Window
{
    private readonly TodoService _todoService;
    private readonly WidgetOpacityManager _opacityManager;
    private ObservableCollection<TodoItem> _todoItems = new();
    private ObservableCollection<TodoItem> _pendingTasks = new();
    private ObservableCollection<TodoItem> _completedTasks = new();
    private ObservableCollection<TodoItem> _deletedTasks = new();
    private ObservableCollection<TodoItem> _archivedTasks = new();
    private ObservableCollection<ArchivedGroup> _archivedGroups = new();
    private Dictionary<string, bool> _archivedExpansionStates = new();
    private DispatcherTimer _mainTimer = new();
    private DateTime _lastAutoSaveTime = DateTime.Now;
    private DateTime _lastOverdueCheckTime = DateTime.Now;
    private DateTime _lastTrashCleanupTime = DateTime.Now;
    private DateTime _lastAutoArchiveCheckTime = DateTime.Now;
    private SystemTrayService? _systemTrayService;
    private GlobalHotKeyService? _globalHotKeyService;
    private bool _isWidgetMode = false;
    private double _widgetWindowWidth = 280;
    private double _widgetWindowHeight = 360;
    private double _widgetWindowLeft = 0;
    private double _widgetWindowTop = 0;
    private bool _isLoaded = false;
    private WidgetWindow? _widgetWindow;
    private QuickAddWindow? _quickAddWindow;

    public MainWindow()
    {
        InitializeComponent();
        _opacityManager = WidgetOpacityManager.Instance;
        _todoService = new TodoService();
        AdjustFontSizeForDpi();
        InitializeData();
        InitializeTimer();
        InitializeSystemTray();
        _opacityManager.OpacityChanged += OnOpacityChanged;
        SettingsService.Instance.SettingsChanged += OnSettingsChanged;
        _isLoaded = true;
        
        SourceInitialized += (s, e) =>
        {
            ApplyNativeWindowAppearance();
            InitializeGlobalHotKey();
        };
    }

    private void ApplyNativeWindowAppearance()
    {
        try
        {
            var helper = new WindowInteropHelper(this);
            if (helper.Handle == IntPtr.Zero)
            {
                return;
            }

            const int darkModeAttribute = 20;
            const int cornerPreferenceAttribute = 33;
            var useDarkMode = 1;
            var cornerPreference = 2;

            NativeMethods.DwmSetWindowAttribute(
                helper.Handle,
                darkModeAttribute,
                ref useDarkMode,
                Marshal.SizeOf<int>());

            NativeMethods.DwmSetWindowAttribute(
                helper.Handle,
                cornerPreferenceAttribute,
                ref cornerPreference,
                Marshal.SizeOf<int>());
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"应用原生窗口外观失败: {ex.Message}");
        }

        UpdateWindowFrameState();
    }

    private void OnOpacityChanged(object? sender, double effectiveOpacity)
    {
    }

    private void InitializeData()
    {
        var items = _todoService.LoadTodos() ?? new ObservableCollection<TodoItem>();
        foreach (var item in items)
        {
            _todoItems.Add(item);
        }

        RefreshTaskCollections();

        if (WidgetView != null)
        {
            WidgetView.TaskChecked += WidgetView_TaskChecked;
            WidgetView.TaskDeleted += WidgetView_TaskDeleted;
            WidgetView.WidgetMouseLeftButtonDown += WidgetView_WidgetMouseLeftButtonDown;
        }

        UpdateTaskCount();
        SetupPlaceholderText();

        CheckAndAutoArchiveCompletedTasks();
        UpdateArchivedGroups();
    }

    private void SetupPlaceholderText()
    {
        if (NewTaskTextBox != null)
        {
            NewTaskTextBox.GotFocus += (s, e) =>
            {
                if (PlaceholderText != null)
                {
                    PlaceholderText.Visibility = Visibility.Collapsed;
                }
            };
            
            NewTaskTextBox.LostFocus += (s, e) =>
            {
                if (PlaceholderText != null && string.IsNullOrEmpty(NewTaskTextBox.Text))
                {
                    PlaceholderText.Visibility = Visibility.Visible;
                }
            };
        }
    }
    
    private void NewTaskTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (PlaceholderText != null)
        {
            PlaceholderText.Visibility = Visibility.Collapsed;
        }
    }
    
    private void NewTaskTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (PlaceholderText != null && string.IsNullOrEmpty(NewTaskTextBox.Text))
        {
            PlaceholderText.Visibility = Visibility.Visible;
        }
    }

    private void NewTaskTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (PlaceholderText != null)
        {
            PlaceholderText.Visibility = string.IsNullOrEmpty(NewTaskTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void TaskTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TaskTabs == null || e.Source != TaskTabs) return;
        
        if (TaskTabs.Template.FindName("PART_SelectedContentHost", TaskTabs) is ContentPresenter contentHost)
        {
            if (Application.Current.TryFindResource("TabSlideInAnimation") is Storyboard storyboard)
            {
                try
                {
                    contentHost.RenderTransform = new TranslateTransform(50, 0);
                    contentHost.Opacity = 0;
                    
                    var clonedStoryboard = storyboard.Clone();
                    clonedStoryboard.Begin(contentHost);
                }
                catch
                {
                    contentHost.RenderTransform = new TranslateTransform(0, 0);
                    contentHost.Opacity = 1;
                }
            }
        }
    }

    private void RefreshTaskCollections()
    {
        _pendingTasks.Clear();
        _completedTasks.Clear();
        _deletedTasks.Clear();
        _archivedTasks.Clear();
        
        var pendingItems = _todoItems.Where(t => !t.IsDeleted && !t.IsArchived && !t.IsCompleted)
            .OrderBy(t => t.DueDate.HasValue ? 0 : 1)
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(t => t.CreatedDate)
            .ToList();
        
        foreach (var item in pendingItems)
        {
            _pendingTasks.Add(item);
        }
        
        foreach (var item in _todoItems.Where(t => !t.IsDeleted && !t.IsArchived && t.IsCompleted).OrderByDescending(t => t.CompletedDate))
        {
            _completedTasks.Add(item);
        }
        
        foreach (var item in _todoItems.Where(t => t.IsDeleted).OrderByDescending(t => t.DeletedDate))
        {
            _deletedTasks.Add(item);
        }
        
        foreach (var item in _todoItems.Where(t => t.IsArchived).OrderByDescending(t => t.ArchivedDate))
        {
            _archivedTasks.Add(item);
        }
        
        if (TasksListBox != null)
        {
            TasksListBox.ItemsSource = _pendingTasks;
        }
        
        if (CompletedTasksListBox != null)
        {
            CompletedTasksListBox.ItemsSource = _completedTasks;
        }
        
        if (DeletedTasksListBox != null)
        {
            DeletedTasksListBox.ItemsSource = _deletedTasks;
        }

        if (ArchivedGroupsListBox != null)
        {
            UpdateArchivedGroups();
        }

        if (WidgetView != null)
        {
            WidgetView.SetTasks(pendingItems);
        }
        
        if (_widgetWindow != null && _widgetWindow.IsVisible)
        {
            _widgetWindow.SetTasks(pendingItems);
        }
    }

    private void InitializeTimer()
    {
        _mainTimer.Interval = TimeSpan.FromSeconds(5);
        _mainTimer.Tick += MainTimer_Tick;
        _mainTimer.Start();
        
        CheckOverdueTasks();
        CleanupExpiredTrashItems();
    }
    
    private void MainTimer_Tick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;

        // 自动保存（每60秒）
        if ((now - _lastAutoSaveTime).TotalSeconds >= 60)
        {
            SaveData();
            _lastAutoSaveTime = now;
        }

        // 过期任务检查（每1小时）
        if ((now - _lastOverdueCheckTime).TotalHours >= 1)
        {
            CheckOverdueTasks();
            _lastOverdueCheckTime = now;
        }

        // 垃圾清理（每1小时）
        if ((now - _lastTrashCleanupTime).TotalHours >= 1)
        {
            CleanupExpiredTrashItems();
            _lastTrashCleanupTime = now;
        }

        // 自动归档检查（每1小时）
        if ((now - _lastAutoArchiveCheckTime).TotalHours >= 1)
        {
            CheckAndAutoArchiveCompletedTasks();
            _lastAutoArchiveCheckTime = now;
        }
    }
    
    private void CleanupExpiredTrashItems()
    {
        var itemsToRemove = _todoItems.Where(t => 
            t.IsDeleted && 
            t.DeletedDate.HasValue && 
            (DateTime.Now - t.DeletedDate.Value).Days >= 7).ToList();
        
        if (itemsToRemove.Any())
        {
            foreach (var item in itemsToRemove)
            {
                _todoItems.Remove(item);
            }
            RefreshTaskCollections();
            UpdateTaskCount();
            SaveData();
            UpdateStatus($"已自动清理 {itemsToRemove.Count} 个过期垃圾箱任务");
        }
    }

    private void CheckAndAutoArchiveCompletedTasks()
    {
        var autoArchiveDays = SettingsService.Instance.Settings.AutoArchiveDays;
        var tasksToArchive = _todoItems.Where(t => 
            !t.IsDeleted && 
            !t.IsArchived && 
            t.IsCompleted && 
            t.CompletedDate.HasValue && 
            (DateTime.Now - t.CompletedDate.Value).Days >= autoArchiveDays).ToList();
        
        if (tasksToArchive.Any())
        {
            foreach (var task in tasksToArchive)
            {
                task.IsArchived = true;
            }
            RefreshTaskCollections();
            UpdateTaskCount();
            SaveData();
            UpdateStatus($"已自动归档 {tasksToArchive.Count} 个已完成任务");
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
            foreach (var yearGroup in _archivedGroups)
            {
                foreach (var monthGroup in yearGroup.Children)
                {
                    foreach (var weekGroup in monthGroup.Children)
                    {
                        var currentWeekNumber = GetCurrentWeekNumber();
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

    private static void CaptureArchivedExpansionStates(ArchivedGroup group, string? parentKey, Dictionary<string, bool> states)
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
        var date = DateTime.Now;
        var firstDayOfYear = new DateTime(date.Year, 1, 1);
        var daysOffset = (int)System.Globalization.CultureInfo.CurrentCulture.Calendar.GetDayOfWeek(firstDayOfYear);
        var firstMonday = firstDayOfYear.AddDays(-daysOffset + (daysOffset <= 3 ? 0 : 7) - 3);
        return System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    private void SaveData()
    {
        if (_todoItems != null)
        {
            _todoService.SaveTodos(_todoItems);
        }
    }

    private void UpdateStatus(string message)
    {
        if (StatusTextBlock != null)
        {
            StatusTextBlock.Text = message;
        }
        
        // 5秒后自动清除状态
        Task.Delay(5000).ContinueWith(_ =>
        {
            Dispatcher.Invoke(() =>
            {
                if (StatusTextBlock != null)
                {
                    StatusTextBlock.Text = "准备就绪";
                }
            });
        });
    }

    private void UpdateTaskCount()
    {
        if (_todoItems == null || TaskCountTextBlock == null) return;
        
        var total = _todoItems.Count;
        var completed = _todoItems.Count(x => x.IsCompleted);
        TaskCountTextBlock.Text = $"({total}项 • 已完成{completed}项)";
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && ResizeMode == ResizeMode.CanResize)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void WidgetView_WidgetMouseLeftButtonDown(object? sender, EventArgs e)
    {
        DragMove();
    }

    private void WidgetView_TaskChecked(object? sender, TodoItem e)
    {
        RefreshTaskCollections();
        UpdateTaskCount();
        SaveData();
    }

    private void WidgetView_TaskDeleted(object? sender, TodoItem e)
    {
        _todoItems.Remove(e);
        RefreshTaskCollections();
        UpdateTaskCount();
        UpdateStatus($"已删除: {e.Title}");
        SaveData();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void SmartReadButton_Click(object sender, RoutedEventArgs e)
    {
        SmartAddTask();
    }

    private void SmartAddTask()
    {
        var input = NewTaskTextBox?.Text?.Trim() ?? "";
        
        if (string.IsNullOrWhiteSpace(input) || input == "添加新的待办事项...")
        {
            UpdateStatus("请输入待办事项内容");
            return;
        }

        var parsedResult = Services.SmartTodoParser.Parse(input);
        
        if (string.IsNullOrWhiteSpace(parsedResult.Title))
        {
            UpdateStatus("无法解析待办事项内容");
            return;
        }

        var todoItem = new TodoItem
        {
            Title = parsedResult.Title,
            CreatedDate = DateTime.Now,
            IsCompleted = false
        };

        if (DueDatePicker?.SelectedDate.HasValue == true)
        {
            todoItem.DueDate = DueDatePicker.SelectedDate.Value;
            todoItem.HasReminder = true;
        }
        else if (parsedResult.DueDate.HasValue)
        {
            todoItem.DueDate = parsedResult.DueDate.Value;
            todoItem.HasReminder = true;
        }

        _todoItems.Insert(0, todoItem);
        RefreshTaskCollections();
        
        // 播放添加动画
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (TasksListBox.ItemContainerGenerator.ContainerFromItem(todoItem) is ListBoxItem listBoxItem)
            {
                // 设置初始状态
                listBoxItem.RenderTransform = new ScaleTransform(0.8, 0.8);
                listBoxItem.RenderTransformOrigin = new Point(0.5, 0.5);
                listBoxItem.Opacity = 0;
                
                PlayAnimationOnItemContainer(listBoxItem, "TaskAddAnimation", null);
            }
        }), DispatcherPriority.Render);
        
        if (NewTaskTextBox != null)
        {
            NewTaskTextBox.Clear();
            PlaceholderText.Visibility = Visibility.Visible;
        }
        
        if (DueDatePicker != null)
        {
            DueDatePicker.SelectedDate = null;
        }
        
        UpdateTaskCount();
        var dateInfo = todoItem.DueDate.HasValue 
            ? $" (截止: {todoItem.DueDate.Value:MM-dd})" 
            : "";
        UpdateStatus($"已添加: {parsedResult.Title}{dateInfo}");
        SaveData();
    }

    private void NewTaskTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SmartAddTask();
        }
    }

    private void TasksListBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
        {
            CopySelectedTask();
            e.Handled = true;
        }
    }

    private void TasksListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        CopySelectedTask();
    }

    private void CopyTask_Click(object sender, RoutedEventArgs e)
    {
        CopySelectedTask();
    }

    private void CopySelectedTask()
    {
        var listBox = TasksListBox;
        if (listBox?.SelectedItem is TodoItem todoItem)
        {
            try
            {
                var text = todoItem.Title;
                if (todoItem.DueDate.HasValue)
                {
                    text += $" {todoItem.DueDate.Value.ToString("yyyy-MM-dd")}";
                }
                Clipboard.SetText(text);
                UpdateStatus("已复制到剪贴板");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"复制失败: {ex.Message}");
            }
        }
        else if (CompletedTasksListBox?.SelectedItem is TodoItem completedItem)
        {
            try
            {
                var text = completedItem.Title;
                if (completedItem.DueDate.HasValue)
                {
                    text += $" {completedItem.DueDate.Value.ToString("yyyy-MM-dd")}";
                }
                Clipboard.SetText(text);
                UpdateStatus("已复制到剪贴板");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"复制失败: {ex.Message}");
            }
        }
    }

    private void EditTask_Click(object sender, RoutedEventArgs e)
    {
        TodoItem? selectedItem = null;
        
        if (TasksListBox?.SelectedItem is TodoItem pendingItem)
        {
            selectedItem = pendingItem;
        }
        else if (CompletedTasksListBox?.SelectedItem is TodoItem completedItem)
        {
            selectedItem = completedItem;
        }

        if (selectedItem == null)
        {
            UpdateStatus("请先选择一个待办事项");
            return;
        }

        var editPanel = new StackPanel
        {
            Margin = new Thickness(0, 8, 0, 8)
        };

        var titleLabel = new TextBlock
        {
            Text = "标题",
            FontSize = 13,
            Foreground = (Brush)Application.Current.Resources["DialogSecondaryForegroundBrush"],
            Margin = new Thickness(0, 0, 0, 6)
        };

        var titleTextBox = new TextBox
        {
            Text = selectedItem.Title,
            FontSize = 14,
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 16)
        };
        
        var titleStyle = Application.Current.Resources["ModernTextBoxStyle"] as Style;
        if (titleStyle != null)
        {
            titleTextBox.Style = titleStyle;
        }

        var dateLabel = new TextBlock
        {
            Text = "截止日期（可选）",
            FontSize = 13,
            Foreground = (Brush)Application.Current.Resources["DialogSecondaryForegroundBrush"],
            Margin = new Thickness(0, 0, 0, 6)
        };

        var datePicker = new global::HandyControl.Controls.DatePicker
        {
            SelectedDate = selectedItem.DueDate,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 8),
            MinWidth = 160
        };

        editPanel.Children.Add(titleLabel);
        editPanel.Children.Add(titleTextBox);
        editPanel.Children.Add(dateLabel);
        editPanel.Children.Add(datePicker);

        var originalTitle = selectedItem.Title;
        var originalDueDate = selectedItem.DueDate;

        DialogService.OnDialogConfirmed = (content) =>
        {
            var panel = content as StackPanel;
            if (panel == null) return false;

            var textBox = panel.Children[1] as TextBox;
            if (textBox == null || string.IsNullOrWhiteSpace(textBox.Text))
            {
                UpdateStatus("标题不能为空");
                return false;
            }

            selectedItem.Title = textBox.Text.Trim();
            
            var picker = panel.Children[3] as global::HandyControl.Controls.DatePicker;
            if (picker != null)
            {
                selectedItem.DueDate = picker.SelectedDate;
            }

            RefreshTaskCollections();
            SaveData();
            UpdateStatus($"已修改: {selectedItem.Title}");
            return true;
        };

        var result = DialogService.ShowCustomDialog("修改待办事项", DialogType.None, editPanel, "保存", "取消");
        
        if (result == Services.DialogResult.Cancel)
        {
            selectedItem.Title = originalTitle;
            selectedItem.DueDate = originalDueDate;
        }

        DialogService.OnDialogConfirmed = null;
    }

    private void TaskCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            // 获取 ListBoxItem 容器
            var listBoxItem = FindParent<ListBoxItem>(checkBox);
            if (listBoxItem != null)
            {
                PlayAnimationOnItemContainer(listBoxItem, "TaskActionAnimation", () =>
                {
                    RefreshTaskCollections();
                    UpdateTaskCount();
                    SaveData();
                });
                return;
            }
        }
        
        // 如果没有动画或动画失败，直接执行
        RefreshTaskCollections();
        UpdateTaskCount();
        SaveData();
    }

    // 播放动画的通用方法
    private void PlayAnimationOnItemContainer(FrameworkElement itemContainer, string resourceKey, Action? onCompleted)
    {
        if (Application.Current.TryFindResource(resourceKey) is Storyboard storyboard)
        {
            try
            {
                // 确保 RenderTransform 已设置
                if (itemContainer.RenderTransform == null || itemContainer.RenderTransform is not ScaleTransform)
                {
                    itemContainer.RenderTransform = new ScaleTransform(1, 1);
                    itemContainer.RenderTransformOrigin = new Point(0.5, 0.5);
                }

                // 克隆动画以避免冲突
                var clonedStoryboard = storyboard.Clone();
                clonedStoryboard.Completed += (s, args) =>
                {
                    onCompleted?.Invoke();
                };
                clonedStoryboard.Begin(itemContainer);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"动画播放失败: {ex.Message}");
                onCompleted?.Invoke();
            }
        }
        else
        {
            onCompleted?.Invoke();
        }
    }

    // 辅助方法：查找父级元素
    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is T typedParent)
                return typedParent;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }

    // 执行操作并播放动画（通过按钮）
    private void ExecuteWithAnimation(Button button, Action action)
    {
        var itemContainer = FindAnimationContainer(button);
        if (itemContainer != null)
        {
            PlayAnimationOnItemContainer(itemContainer, "TaskActionAnimation", action);
        }
        else
        {
            action();
        }
    }

    // 执行操作并播放动画（通过ListBox和选中项）
    private void ExecuteWithAnimation(ListBox listBox, TodoItem todoItem, Action action)
    {
        var listBoxItem = listBox.ItemContainerGenerator.ContainerFromItem(todoItem) as ListBoxItem;
        if (listBoxItem != null)
        {
            PlayAnimationOnItemContainer(listBoxItem, "TaskActionAnimation", action);
        }
        else
        {
            action();
        }
    }

    private void ExecuteWithAnimation(TreeView treeView, TodoItem todoItem, Action action)
    {
        treeView.UpdateLayout();
        var treeViewItem = FindTreeViewItem(treeView, todoItem);
        if (treeViewItem != null)
        {
            PlayAnimationOnItemContainer(treeViewItem, "TaskActionAnimation", action);
        }
        else
        {
            action();
        }
    }

    private static FrameworkElement? FindAnimationContainer(DependencyObject child)
    {
        var listBoxItem = FindParent<ListBoxItem>(child);
        if (listBoxItem != null)
        {
            return listBoxItem;
        }

        return FindParent<TreeViewItem>(child);
    }

    private static TreeViewItem? FindTreeViewItem(ItemsControl parent, object item)
    {
        if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem directItem)
        {
            return directItem;
        }

        foreach (var child in parent.Items)
        {
            if (parent.ItemContainerGenerator.ContainerFromItem(child) is not TreeViewItem treeViewItem)
            {
                continue;
            }

            var nestedItem = FindTreeViewItem(treeViewItem, item);
            if (nestedItem != null)
            {
                return nestedItem;
            }
        }

        return null;
    }

    // 批量执行动画（用于全部恢复/清空等操作）
    private void ExecuteBatchAnimation(ListBox listBox, List<TodoItem> items, Action onCompleted)
    {
        if (items.Count == 0)
        {
            onCompleted?.Invoke();
            return;
        }

        var listBoxItems = new List<ListBoxItem>();
        foreach (var item in items)
        {
            var listBoxItem = listBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
            if (listBoxItem != null)
            {
                listBoxItems.Add(listBoxItem);
            }
        }

        if (listBoxItems.Count == 0)
        {
            onCompleted?.Invoke();
            return;
        }

        if (Application.Current.TryFindResource("TaskActionAnimation") is Storyboard storyboard)
        {
            int completedCount = 0;
            int totalCount = listBoxItems.Count;

            foreach (var listBoxItem in listBoxItems)
            {
                try
                {
                    var clonedStoryboard = storyboard.Clone();
                    clonedStoryboard.Completed += (s, args) =>
                    {
                        completedCount++;
                        if (completedCount >= totalCount)
                        {
                            onCompleted?.Invoke();
                        }
                    };
                    clonedStoryboard.Begin(listBoxItem);
                }
                catch
                {
                    completedCount++;
                    if (completedCount >= totalCount)
                    {
                        onCompleted?.Invoke();
                    }
                }
            }
        }
        else
        {
            onCompleted?.Invoke();
        }
    }

    private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TodoItem todoItem)
        {
            var title = todoItem.Title;
            ExecuteWithAnimation(button, () =>
            {
                todoItem.IsDeleted = true;
                RefreshTaskCollections();
                UpdateTaskCount();
                UpdateStatus($"已移至垃圾箱: {title}");
                SaveData();
            });
        }
    }

    private void RestoreTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TodoItem todoItem)
        {
            var title = todoItem.Title;
            ExecuteWithAnimation(button, () =>
            {
                todoItem.IsDeleted = false;
                RefreshTaskCollections();
                UpdateTaskCount();
                UpdateStatus($"已恢复: {title}");
                SaveData();
            });
        }
    }

    private void RestoreTask_Click(object sender, RoutedEventArgs e)
    {
        var listBox = DeletedTasksListBox;
        if (listBox?.SelectedItem is TodoItem todoItem)
        {
            var title = todoItem.Title;
            ExecuteWithAnimation(listBox, todoItem, () =>
            {
                todoItem.IsDeleted = false;
                RefreshTaskCollections();
                UpdateTaskCount();
                UpdateStatus($"已恢复: {title}");
                SaveData();
            });
        }
    }

    private void PermanentDeleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TodoItem todoItem)
        {
            var title = todoItem.Title;
            ExecuteWithAnimation(button, () =>
            {
                _todoItems.Remove(todoItem);
                RefreshTaskCollections();
                UpdateTaskCount();
                UpdateStatus($"已永久删除: {title}");
                SaveData();
            });
        }
    }

    private void PermanentDeleteTask_Click(object sender, RoutedEventArgs e)
    {
        var listBox = DeletedTasksListBox;
        if (listBox?.SelectedItem is TodoItem todoItem)
        {
            var title = todoItem.Title;
            ExecuteWithAnimation(listBox, todoItem, () =>
            {
                _todoItems.Remove(todoItem);
                RefreshTaskCollections();
                UpdateTaskCount();
                UpdateStatus($"已永久删除: {title}");
                SaveData();
            });
        }
    }
    
    private void EmptyTrashButton_Click(object sender, RoutedEventArgs e)
    {
        var result = DialogService.ShowConfirm("确定要清空垃圾箱吗？所有任务将被永久删除！", "确认清空");
        if (result == Services.DialogResult.OK)
        {
            var itemsToRemove = _todoItems.Where(t => t.IsDeleted).ToList();
            ExecuteBatchAnimation(DeletedTasksListBox, itemsToRemove, () =>
            {
                foreach (var item in itemsToRemove)
                {
                    _todoItems.Remove(item);
                }
                RefreshTaskCollections();
                UpdateTaskCount();
                UpdateStatus("垃圾箱已清空");
                SaveData();
            });
        }
    }
    
    private void ArchiveTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TodoItem todoItem)
        {
            var title = todoItem.Title;
            ExecuteWithAnimation(button, () =>
            {
                todoItem.IsArchived = true;
                RefreshTaskCollections();
                UpdateStatus($"已归档: {title}");
                SaveData();
            });
        }
    }
    
    private void ArchiveTask_Click(object sender, RoutedEventArgs e)
    {
        var listBox = CompletedTasksListBox;
        if (listBox?.SelectedItem is TodoItem todoItem)
        {
            var title = todoItem.Title;
            ExecuteWithAnimation(listBox, todoItem, () =>
            {
                todoItem.IsArchived = true;
                RefreshTaskCollections();
                UpdateStatus($"已归档: {title}");
                SaveData();
            });
        }
    }

    private void UnarchiveTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TodoItem todoItem)
        {
            var title = todoItem.Title;
            ExecuteWithAnimation(button, () =>
            {
                todoItem.IsArchived = false;
                RefreshTaskCollections();
                UpdateStatus($"已取消归档: {title}");
                SaveData();
            });
        }
    }

    private void UnarchiveTask_Click(object sender, RoutedEventArgs e)
    {
        if (ArchivedGroupsListBox?.SelectedItem is TodoItem todoItem)
        {
            var title = todoItem.Title;
            ExecuteWithAnimation(ArchivedGroupsListBox, todoItem, () =>
            {
                todoItem.IsArchived = false;
                RefreshTaskCollections();
                UpdateStatus($"已取消归档: {title}");
                SaveData();
            });
        }
    }

    private void YearExpandButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton button && button.Tag is ArchivedGroup group)
        {
            if (button.IsChecked == true)
            {
                group.ExpandAll();
            }
            else
            {
                group.CollapseAll();
            }
        }
    }

    private void UnarchiveGroupTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ArchivedGroup group)
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
        }
    }

    private void UnarchiveAllButton_Click(object sender, RoutedEventArgs e)
    {
        var result = DialogService.ShowConfirm("确定要恢复所有归档的任务吗？", "确认恢复");
        if (result == Services.DialogResult.OK)
        {
            var itemsToRestore = _todoItems.Where(t => t.IsArchived).ToList();
            foreach (var item in itemsToRestore)
            {
                item.IsArchived = false;
            }
            RefreshTaskCollections();
            UpdateTaskCount();
            UpdateStatus("所有归档任务已恢复");
            SaveData();
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

    private void AddNewTask()
    {
        var title = NewTaskTextBox?.Text?.Trim() ?? "";
        
        if (string.IsNullOrWhiteSpace(title) || title == "添加新的待办事项...")
        {
            UpdateStatus("请输入待办事项内容");
            return;
        }

        var todoItem = new TodoItem
        {
            Title = title,
            CreatedDate = DateTime.Now,
            IsCompleted = false
        };

        if (DueDatePicker?.SelectedDate.HasValue == true)
        {
            todoItem.DueDate = DueDatePicker.SelectedDate.Value;
            todoItem.HasReminder = true;
        }

        _todoItems.Insert(0, todoItem);
        RefreshTaskCollections();
        
        if (NewTaskTextBox != null)
        {
            NewTaskTextBox.Clear();
            PlaceholderText.Visibility = Visibility.Visible;
        }
        
        if (DueDatePicker != null)
        {
            DueDatePicker.SelectedDate = null;
        }
        
        UpdateTaskCount();
        UpdateStatus($"已添加: {title}");
        SaveData();
    }

    private void ClearDateButton_Click(object sender, RoutedEventArgs e)
    {
        if (DueDatePicker != null)
        {
            DueDatePicker.SelectedDate = null;
            UpdateStatus("已清除截止日期");
        }
    }

    private void DueDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DueDatePicker?.SelectedDate != null && DueDatePicker.SelectedDate < DateTime.Now.Date)
        {
            UpdateStatus("提醒：选择的日期已过期");
        }
    }

    private void WidgetModeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleWidgetMode();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow
        {
            Owner = this
        };
        settingsWindow.ShowDialog();
    }

    private void ToggleWidgetMode()
    {
        if (_isWidgetMode)
        {
            ExitWidgetMode();
        }
        else
        {
            EnterWidgetMode();
        }
    }
    
    public void EnterWidgetMode()
    {
        if (_isWidgetMode) return;
        
        try
        {
            _opacityManager.IsWidgetMode = true;
            EnterWidgetModeInternal();
            _isWidgetMode = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"进入小组件模式失败: {ex.Message}");
            _isWidgetMode = false;
            _opacityManager.IsWidgetMode = false;
            RestoreTaskbarIcon();
            Opacity = 1.0;
            Show();
            Activate();
        }
    }

    private void ExitWidgetMode()
    {
        if (!_isWidgetMode) return;

        try
        {
            ExitWidgetModeInternal();
            _isWidgetMode = false;
            _opacityManager.IsWidgetMode = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"退出小组件模式失败: {ex.Message}");
            _isWidgetMode = true;
            _opacityManager.IsWidgetMode = true;
        }
    }
    
    private void EnterWidgetModeInternal()
    {
        if (_widgetWindow == null)
        {
            _widgetWindow = new WidgetWindow();
            _widgetWindow.SetMainWindow(this);
            _widgetWindow.TaskChecked += OnWidgetTaskChecked;
        }
        
        var pendingItems = _todoItems.Where(t => !t.IsDeleted && !t.IsArchived && !t.IsCompleted)
            .OrderBy(t => t.DueDate.HasValue ? 0 : 1)
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(t => t.CreatedDate)
            .ToList();
        _widgetWindow.SetTasks(pendingItems);
        
        if (_widgetWindowLeft > 0 && _widgetWindowTop > 0)
        {
            _widgetWindow.Left = _widgetWindowLeft;
            _widgetWindow.Top = _widgetWindowTop;
            _widgetWindow.Width = _widgetWindowWidth;
            _widgetWindow.Height = _widgetWindowHeight;
        }
        else
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var taskbarHeight = SystemParameters.WorkArea.Top;
            var menuBarHeight = Math.Max(30, taskbarHeight);
            
            _widgetWindow.Width = _widgetWindowWidth;
            _widgetWindow.Height = _widgetWindowHeight;
            _widgetWindow.Left = screenWidth - _widgetWindowWidth - 20;
            _widgetWindow.Top = menuBarHeight + 10;
        }
        
        _widgetWindow.Show();
        
        HideTaskbarIcon();
        Hide();
        
        UpdateStatus("已切换到小组件模式");
    }
    
    private void ExitWidgetModeInternal()
    {
        if (_widgetWindow != null)
        {
            _widgetWindowLeft = _widgetWindow.Left;
            _widgetWindowTop = _widgetWindow.Top;
            _widgetWindowWidth = _widgetWindow.Width;
            _widgetWindowHeight = _widgetWindow.Height;
            _widgetWindow.Hide();
        }
        
        var helper = new WindowInteropHelper(this);
        if (helper.Handle != IntPtr.Zero)
        {
            int currentStyle = NativeMethods.GetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE);
            currentStyle &= ~NativeMethods.WS_EX_TRANSPARENT;
            NativeMethods.SetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE, currentStyle);
        }
        
        Opacity = 1.0;
        IsHitTestVisible = true;
        
        RestoreTaskbarIcon();
        Show();
        WindowState = WindowState.Normal;
        Activate();
        
        UpdateStatus("已切换到主页面");
    }
    
    private void OnWidgetTaskChecked(object? sender, TodoItem todoItem)
    {
        Dispatcher.Invoke(() =>
        {
            var item = _todoItems.FirstOrDefault(t => 
                t.Title == todoItem.Title && 
                t.DueDate == todoItem.DueDate);
            if (item != null)
            {
                item.IsCompleted = todoItem.IsCompleted;
                RefreshTaskCollections();
                _todoService.SaveTodos(_todoItems);
            }
        });
    }
    
    private void HideTaskbarIcon()
    {
        var helper = new WindowInteropHelper(this);
        if (helper.Handle != IntPtr.Zero)
        {
            int currentStyle = NativeMethods.GetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE, 
                currentStyle | NativeMethods.WS_EX_TOOLWINDOW);
        }
        ShowInTaskbar = false;
    }
    
    private void RestoreTaskbarIcon()
    {
        var helper = new WindowInteropHelper(this);
        if (helper.Handle != IntPtr.Zero)
        {
            int currentStyle = NativeMethods.GetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE, 
                currentStyle & ~NativeMethods.WS_EX_TOOLWINDOW);
        }
        ShowInTaskbar = true;
    }

    private void CheckOverdueTasks()
    {
        var overdueTasks = _todoItems.Where(t => t.IsOverdue).ToList();
        
        if (overdueTasks.Any())
        {
            var taskCount = overdueTasks.Count;
            var message = taskCount == 1 
                ? $"有 1 个任务已过期：{overdueTasks.First().Title}" 
                : $"有 {taskCount} 个任务已过期";
            
            UpdateStatus(message);
            _systemTrayService?.ShowNotification("待办事项提醒", message);
        }
    }

    private void AdjustFontSizeForDpi()
    {
        var dpiScale = VisualTreeHelper.GetDpi(this);
        var dpiFactor = dpiScale.DpiScaleX;
        
        var baseFontSize = 14;
        if (dpiFactor > 1.5)
        {
            baseFontSize = 16;
        }
        else if (dpiFactor > 1.25)
        {
            baseFontSize = 15;
        }
        
        FontSize = baseFontSize;
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

    private void InitializeGlobalHotKey()
    {
        try
        {
            _globalHotKeyService = new GlobalHotKeyService(this);
            _globalHotKeyService.HotKeyPressed += OnGlobalHotKeyPressed;
            
            var settings = SettingsService.Instance.Settings;
            int hotKeyId = _globalHotKeyService.RegisterHotKey(
                settings.HotKeyModifiers,
                settings.HotKeyKey
            );

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

                _quickAddWindow = new Views.QuickAddWindow(_todoService, _todoItems)
                {
                    Owner = this
                };
                _quickAddWindow.Closed += (s, e) => _quickAddWindow = null;
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
            if (_globalHotKeyService != null && _isLoaded)
            {
                var settings = SettingsService.Instance.Settings;
                int hotKeyId = _globalHotKeyService.RegisterHotKey(
                    settings.HotKeyModifiers,
                    settings.HotKeyKey
                );

                if (hotKeyId != -1)
                {
                    UpdateStatus($"全局快捷键已更新：{_globalHotKeyService.GetHotKeyDisplayText()}");
                }
                else
                {
                    UpdateStatus("全局快捷键更新失败");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"更新全局快捷键失败: {ex.Message}");
        }
    }

    private void SetWidgetDefaultPosition()
    {
        try
        {
            var workingArea = SystemParameters.WorkArea;
            var margin = 20;
            var left = workingArea.Right - Width - margin;
            var top = workingArea.Top + margin;
            
            left = Math.Max(workingArea.Left + margin, left);
            top = Math.Max(workingArea.Top + margin, top);
            
            Left = left;
            Top = top;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"设置小组件位置失败: {ex.Message}");
        }
    }

    private void EnableMouseInteraction()
    {
        IsHitTestVisible = true;
        
        var helper = new WindowInteropHelper(this);
        if (helper.Handle != IntPtr.Zero)
        {
            int currentStyle = NativeMethods.GetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE, 
                currentStyle & ~NativeMethods.WS_EX_TRANSPARENT);
        }
        
        _opacityManager.IsMousePassThroughEnabled = false;
        
        if (_isWidgetMode)
        {
            Opacity = _opacityManager.EffectiveOpacity;
        }
    }

    private void EnableMousePassThrough()
    {
        IsHitTestVisible = true;
        
        var helper = new WindowInteropHelper(this);
        if (helper.Handle != IntPtr.Zero)
        {
            int currentStyle = NativeMethods.GetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE);
            currentStyle &= ~NativeMethods.WS_EX_TRANSPARENT;
            NativeMethods.SetWindowLong(helper.Handle, NativeMethods.GWL_EXSTYLE, 
                currentStyle | NativeMethods.WS_EX_TRANSPARENT);
        }
        
        _opacityManager.IsMousePassThroughEnabled = true;
        
        if (_isWidgetMode)
        {
            Opacity = _opacityManager.EffectiveOpacity;
        }
    }

    public bool IsMousePassThroughEnabled()
    {
        return _opacityManager.IsMousePassThroughEnabled;
    }

    public bool IsWidgetMode()
    {
        return _isWidgetMode;
    }

    public void ToggleMousePassThrough()
    {
        _opacityManager.IsMousePassThroughEnabled = !_opacityManager.IsMousePassThroughEnabled;
        
        if (_opacityManager.IsMousePassThroughEnabled)
        {
            UpdateStatus("已进入沉浸模式");
        }
        else
        {
            UpdateStatus("已退出沉浸模式");
        }
    }

    public void RestoreMainWindow()
    {
        if (_isWidgetMode)
        {
            Show();
            Activate();
            return;
        }

        RestoreTaskbarIcon();

        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        BringWindowToFront();
    }

    public Task RestoreFromTrayAnimatedAsync()
    {
        RestoreMainWindow();
        return Task.CompletedTask;
    }

    private void BringWindowToFront()
    {
        Activate();

        var helper = new WindowInteropHelper(this);
        if (helper.Handle != IntPtr.Zero)
        {
            NativeMethods.ShowWindow(helper.Handle, NativeMethods.SW_RESTORE);
            NativeMethods.SetForegroundWindow(helper.Handle);
        }
    }

    private void UpdateWindowFrameState()
    {
        if (WindowState == WindowState.Maximized)
        {
            MainBorder.CornerRadius = new CornerRadius(0);
            MainBorder.Margin = new Thickness(8);
            return;
        }

        MainBorder.CornerRadius = new CornerRadius(8);
        MainBorder.Margin = new Thickness(0);
    }

    private void EnsureWindowInScreen()
    {
        try
        {
            var workingArea = SystemParameters.WorkArea;
            
            if (Left < workingArea.Left)
            {
                Left = workingArea.Left;
            }
            
            if (Top < workingArea.Top)
            {
                Top = workingArea.Top;
            }
            
            if (Left + Width > workingArea.Right)
            {
                Left = workingArea.Right - Width;
            }
            
            if (Top + Height > workingArea.Bottom)
            {
                Top = workingArea.Bottom - Height;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"调整窗口位置失败: {ex.Message}");
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_isLoaded)
        {
            SaveData();
            _mainTimer.Stop();
            _systemTrayService?.Dispose();
            _globalHotKeyService?.Dispose();
        }
        base.OnClosed(e);
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        UpdateWindowFrameState();
    }

    private static class NativeMethods
    {
        public const int SW_RESTORE = 9;
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
    }
}
