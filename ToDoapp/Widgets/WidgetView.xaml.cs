using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.Widgets;

public partial class WidgetView : UserControl
{
    public event EventHandler<TodoItem>? TaskChecked;
    public event EventHandler<TodoItem>? TaskDeleted;
    public event EventHandler? WidgetMouseLeftButtonDown;

    private bool _isResizing = false;
    private Point _resizeStartPoint;
    private double _resizeStartWidth;
    private double _resizeStartHeight;
    private const double ResizeAreaThreshold = 20;
    private SolidColorBrush? _backgroundBrush;

    public WidgetView()
    {
        InitializeComponent();
        Loaded += WidgetView_Loaded;
    }

    private void WidgetView_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshThemeBrushes();

        var opacityManager = WidgetOpacityManager.Instance;
        opacityManager.OpacityChanged += OnOpacityManagerChanged;
        opacityManager.ContentOpacityChanged += OnContentOpacityManagerChanged;
        UpdateOpacity();
        UpdateContentOpacity();
    }

    public void RefreshThemeBrushes()
    {
        var color = GetResourceColor("WidgetBackgroundBrush", Color.FromRgb(32, 32, 32));
        _backgroundBrush = new SolidColorBrush(color);
        MainBorder.Background = _backgroundBrush;
        MainBorder.BorderBrush = Application.Current.TryFindResource("BorderBrush") as Brush ?? MainBorder.BorderBrush;
        UpdateOpacity();
    }

    private static Color GetResourceColor(string key, Color fallback)
    {
        return Application.Current.TryFindResource(key) is SolidColorBrush brush
            ? brush.Color
            : fallback;
    }

    private void OnOpacityManagerChanged(object? sender, double effectiveOpacity)
    {
        UpdateOpacity();
    }

    private void OnContentOpacityManagerChanged(object? sender, double effectiveContentOpacity)
    {
        UpdateContentOpacity();
    }

    private void UpdateOpacity()
    {
        var opacityManager = WidgetOpacityManager.Instance;
        var effectiveOpacity = opacityManager.IsMousePassThroughEnabled
            ? opacityManager.EffectiveOpacity
            : 1.0;

        if (_backgroundBrush != null)
        {
            var color = _backgroundBrush.Color;
            color.A = (byte)(255 * effectiveOpacity);
            _backgroundBrush.Color = color;
        }
    }

    private void UpdateContentOpacity()
    {
        var opacityManager = WidgetOpacityManager.Instance;
        var contentOpacity = opacityManager.IsMousePassThroughEnabled
            ? opacityManager.EffectiveContentOpacity
            : 1.0;

        if (WidgetTasksListBox != null)
        {
            WidgetTasksListBox.Opacity = contentOpacity;
        }
        if (EmptyMessage != null)
        {
            EmptyMessage.Opacity = contentOpacity;
        }
    }

    public void SetTasks(IEnumerable<TodoItem> tasks)
    {
        WidgetTasksListBox.ItemsSource = tasks;
    }

    private void TaskCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is TodoItem todoItem)
        {
            TaskChecked?.Invoke(this, todoItem);
        }
    }

    private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TodoItem todoItem)
        {
            TaskDeleted?.Invoke(this, todoItem);
        }
    }

    private void WidgetView_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // 检查是否点击了按钮或复选框
        var clickedElement = e.OriginalSource as DependencyObject;
        while (clickedElement != null && !(clickedElement is Button) && !(clickedElement is CheckBox))
        {
            clickedElement = VisualTreeHelper.GetParent(clickedElement);
        }
        
        // 如果点击了按钮或复选框，不处理拖动操作
        if (clickedElement is Button || clickedElement is CheckBox)
        {
            return;
        }

        var window = Window.GetWindow(this);
        if (window == null) return;

        // 计算鼠标在窗口中的位置
        Point mousePosition = e.GetPosition(window);

        // 检测鼠标是否在右下角调整大小区域
        bool isInResizeArea = mousePosition.X >= window.ActualWidth - ResizeAreaThreshold &&
                              mousePosition.Y >= window.ActualHeight - ResizeAreaThreshold;

        // 如果不在调整大小区域，才触发拖动操作
        if (!isInResizeArea)
        {
            e.Handled = true;
            WidgetMouseLeftButtonDown?.Invoke(this, EventArgs.Empty);
        }
    }

    private void WidgetView_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window == null) return;

        // 计算鼠标在窗口中的位置
        Point mousePosition = e.GetPosition(window);

        if (_isResizing)
        {
            // 正在调整大小
            Point currentPoint = e.GetPosition(window);
            double widthChange = currentPoint.X - _resizeStartPoint.X;
            double heightChange = currentPoint.Y - _resizeStartPoint.Y;

            // 调整窗口大小
            window.Width = Math.Max(200, _resizeStartWidth + widthChange);
            window.Height = Math.Max(150, _resizeStartHeight + heightChange);
        }
        else
        {
            // 检测鼠标是否在右下角调整大小区域
            bool isInResizeArea = mousePosition.X >= window.ActualWidth - ResizeAreaThreshold &&
                                  mousePosition.Y >= window.ActualHeight - ResizeAreaThreshold;

            // 设置光标
            if (isInResizeArea)
            {
                Cursor = Cursors.SizeNWSE;
            }
            else
            {
                Cursor = Cursors.Arrow;
            }
        }
    }

    private void WidgetView_MouseLeftButtonDownForResize(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var window = Window.GetWindow(this);
        if (window == null) return;

        // 计算鼠标在窗口中的位置
        Point mousePosition = e.GetPosition(window);

        // 检测鼠标是否在右下角调整大小区域
        bool isInResizeArea = mousePosition.X >= window.ActualWidth - ResizeAreaThreshold &&
                              mousePosition.Y >= window.ActualHeight - ResizeAreaThreshold;

        if (isInResizeArea)
        {
            // 开始调整大小
            _isResizing = true;
            _resizeStartPoint = mousePosition;
            _resizeStartWidth = window.Width;
            _resizeStartHeight = window.Height;
            Mouse.Capture(this);
        }
    }

    private void WidgetView_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // 结束调整大小
        if (_isResizing)
        {
            _isResizing = false;
            Mouse.Capture(null);
        }
    }
}
