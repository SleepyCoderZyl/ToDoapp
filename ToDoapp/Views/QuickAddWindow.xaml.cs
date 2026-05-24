using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        InputTextBox.Focus();
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
        
        PreviewDateText.Text = parsedResult.DueDate.HasValue 
            ? FormatPreviewDateText(parsedResult)
            : "无";
    }

    private static string FormatPreviewDateText(SmartTodoParser.ParsedTodoResult parsedResult)
    {
        var dateText = parsedResult.DueDate?.ToString("yyyy-MM-dd") ?? "无";
        return string.IsNullOrWhiteSpace(parsedResult.DateSourceHint)
            ? dateText
            : $"{dateText} · {parsedResult.DateSourceHint}";
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
            todoItem.HasReminder = true;
        }

        _todoItems.Insert(0, todoItem);
        _todoService.SaveTodos(_todoItems);
        
        DialogResult = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TitleText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
