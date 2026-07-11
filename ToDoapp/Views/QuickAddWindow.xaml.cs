using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.Views;

public partial class QuickAddWindow : Window
{
    private readonly ITodoService _todoService;
    private readonly ObservableCollection<TodoItem> _todoItems;

    public QuickAddWindow(ITodoService todoService, ObservableCollection<TodoItem> todoItems)
    {
        InitializeComponent();
        _todoService = todoService;
        _todoItems = todoItems;
        
        Loaded += QuickAddWindow_Loaded;
    }

    private void QuickAddWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWindow();
        Dispatcher.BeginInvoke(FocusInput, DispatcherPriority.ApplicationIdle);
    }

    public void FocusInput()
    {
        Activate();
        InputTextBox.Focus();
        Keyboard.Focus(InputTextBox);
        InputTextBox.CaretIndex = InputTextBox.Text.Length;
    }

    private void PositionWindow()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var workArea = SystemParameters.WorkArea;
        
        Left = (screenWidth - Width) / 2;
        Top = workArea.Bottom - Height - 100;
    }

    private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var input = InputTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(input))
        {
            PreviewTitleText.Text = "等待输入...";
            PreviewDateText.Text = "无";
            return;
        }

        var parsedResult = SmartTodoParser.Parse(input);

        PreviewTitleText.Text = string.IsNullOrWhiteSpace(parsedResult.Title)
            ? "无法解析内容"
            : parsedResult.Title;

        PreviewDateText.Text = BuildPreviewSummary(parsedResult);
    }

    private static string BuildPreviewSummary(SmartTodoParser.ParsedTodoResult parsedResult)
    {
        var parts = new List<string>();

        if (parsedResult.DueDate.HasValue)
        {
            var dateText = parsedResult.DueDate.Value.ToString("yyyy-MM-dd");
            if (!string.IsNullOrWhiteSpace(parsedResult.DateSourceHint))
            {
                dateText += $"（{parsedResult.DateSourceHint}）";
            }
            parts.Add($"日期: {dateText}");
        }

        if (parsedResult.DueTime.HasValue)
        {
            var timeText = parsedResult.DueTime.Value.ToString("HH:mm");
            if (!string.IsNullOrWhiteSpace(parsedResult.TimeSourceHint))
            {
                timeText += $"（{parsedResult.TimeSourceHint}）";
            }
            parts.Add($"时间: {timeText}");
        }

        if (parsedResult.ReminderOffsetMinutes.HasValue)
        {
            var offsetText = $"提前 {parsedResult.ReminderOffsetMinutes.Value} 分钟";
            if (!string.IsNullOrWhiteSpace(parsedResult.OffsetSourceHint))
            {
                offsetText += $"（{parsedResult.OffsetSourceHint}）";
            }
            parts.Add($"提醒: {offsetText}");
        }

        return parts.Count == 0 ? "无" : string.Join("  ·  ", parts);
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddButton_Click(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CloseButton_Click(sender, e);
            e.Handled = true;
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var input = InputTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        var parsedResult = SmartTodoParser.Parse(input);

        if (string.IsNullOrWhiteSpace(parsedResult.Title))
        {
            return;
        }

        var todoItem = new TodoItem
        {
            Title = parsedResult.Title,
            CreatedDate = DateTime.Now,
            IsCompleted = false
        };

        if (parsedResult.DueDate.HasValue)
        {
            todoItem.DueDate = parsedResult.DueDate.Value;
            todoItem.DueTime = parsedResult.DueTime;
            todoItem.ReminderOffsetMinutes = parsedResult.ReminderOffsetMinutes;
            // 仅当解析得到具体时间时，才开启提醒
            todoItem.HasReminder = parsedResult.DueTime.HasValue;
        }

        _todoItems.Insert(0, todoItem);
        var saveResult = _todoService.SaveTodos(_todoItems);
        if (!saveResult.IsSuccess)
        {
            _todoItems.Remove(todoItem);
            MessageBox.Show(
                this,
                saveResult.ErrorMessage ?? "保存待办事项失败，请稍后重试。",
                "快速添加失败",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);

        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        var source = e.OriginalSource as DependencyObject;
        if (source == null)
            return;

        // 排除输入框及其子元素
        if (IsDescendantOf(source, InputTextBox))
            return;

        // 排除按钮及其子元素
        if (FindAncestorOfType<Button>(source) != null)
            return;

        DragMove();
    }

    private static bool IsDescendantOf(DependencyObject? node, DependencyObject? ancestor)
    {
        while (node != null)
        {
            if (node == ancestor)
                return true;
            node = VisualTreeHelper.GetParent(node);
        }
        return false;
    }

    private static T? FindAncestorOfType<T>(DependencyObject? node) where T : DependencyObject
    {
        while (node != null)
        {
            if (node is T t)
                return t;
            node = VisualTreeHelper.GetParent(node);
        }
        return null;
    }
}
