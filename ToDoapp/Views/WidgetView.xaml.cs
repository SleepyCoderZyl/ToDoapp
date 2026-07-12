using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.Views;

public partial class WidgetView : UserControl
{
    public bool AreAnimationsEnabled => SystemParameters.ClientAreaAnimation;

    public event EventHandler<TodoItem>? TaskChecked;
    public event EventHandler<TodoItem>? TaskDeleted;
    public event EventHandler? WidgetMouseLeftButtonDown;

    private bool _isResizing = false;
    private Point _resizeStartPoint;
    private double _resizeStartWidth;
    private double _resizeStartHeight;
    private const double ResizeAreaThreshold = 20;
    private bool _isSubscribedToOpacityManager;

    public static readonly DependencyProperty IsMousePassThroughEnabledProperty =
        DependencyProperty.Register(
            nameof(IsMousePassThroughEnabled),
            typeof(bool),
            typeof(WidgetView),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsWindowInteractionEnabledProperty =
        DependencyProperty.Register(
            nameof(IsWindowInteractionEnabled),
            typeof(bool),
            typeof(WidgetView),
            new PropertyMetadata(true));

    public bool IsMousePassThroughEnabled
    {
        get => (bool)GetValue(IsMousePassThroughEnabledProperty);
        set => SetValue(IsMousePassThroughEnabledProperty, value);
    }

    /// <summary>
    /// 是否启用窗口交互（拖动、调整大小）。当 WidgetView 被嵌入 WidgetWindow 时设为 False，
    /// 因为 WidgetWindow 自身处理这些交互。
    /// </summary>
    public bool IsWindowInteractionEnabled
    {
        get => (bool)GetValue(IsWindowInteractionEnabledProperty);
        set => SetValue(IsWindowInteractionEnabledProperty, value);
    }

    public WidgetView()
    {
        InitializeComponent();
        Loaded += WidgetView_Loaded;
        Unloaded += WidgetView_Unloaded;
    }

    private void WidgetView_Loaded(object sender, RoutedEventArgs e)
    {
        EnsureOpacityManagerSubscribed();
        UpdateOpacity();
        UpdateContentOpacity();
    }

    private void WidgetView_Unloaded(object sender, RoutedEventArgs e)
    {
        UnsubscribeFromOpacityManager();
    }

    private void EnsureOpacityManagerSubscribed()
    {
        if (_isSubscribedToOpacityManager) return;

        var opacityManager = WidgetOpacityManager.Instance;
        opacityManager.OpacityChanged += OnOpacityManagerChanged;
        opacityManager.ContentOpacityChanged += OnContentOpacityManagerChanged;
        opacityManager.PropertyChanged += OnOpacityManagerPropertyChanged;
        IsMousePassThroughEnabled = opacityManager.IsMousePassThroughEnabled;
        _isSubscribedToOpacityManager = true;
    }

    private void UnsubscribeFromOpacityManager()
    {
        if (!_isSubscribedToOpacityManager) return;

        var opacityManager = WidgetOpacityManager.Instance;
        opacityManager.OpacityChanged -= OnOpacityManagerChanged;
        opacityManager.ContentOpacityChanged -= OnContentOpacityManagerChanged;
        opacityManager.PropertyChanged -= OnOpacityManagerPropertyChanged;
        _isSubscribedToOpacityManager = false;
    }

    private void OnOpacityManagerChanged(object? sender, double effectiveOpacity)
    {
        UpdateOpacity();
    }

    private void OnContentOpacityManagerChanged(object? sender, double effectiveContentOpacity)
    {
        UpdateContentOpacity();
    }

    private void OnOpacityManagerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WidgetOpacityManager.IsMousePassThroughEnabled))
        {
            IsMousePassThroughEnabled = WidgetOpacityManager.Instance.IsMousePassThroughEnabled;
        }
    }

    private void UpdateOpacity()
    {
        var opacityManager = WidgetOpacityManager.Instance;
        var effectiveOpacity = opacityManager.IsMousePassThroughEnabled
            ? opacityManager.EffectiveOpacity
            : 1.0;

        if (MainBorder != null)
        {
            MainBorder.Opacity = effectiveOpacity;
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
        var taskList = tasks is IList<TodoItem> list ? list : new List<TodoItem>(tasks);
        if (EmptyMessage != null)
        {
            EmptyMessage.Visibility = taskList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void TaskCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is TodoItem todoItem)
        {
            var listBoxItem = FindParent<ListBoxItem>(checkBox);
            if (listBoxItem != null)
            {
                PlayAnimationOnListBoxItem(listBoxItem, () =>
                {
                    TaskChecked?.Invoke(this, todoItem);
                });
            }
            else
            {
                TaskChecked?.Invoke(this, todoItem);
            }
        }
    }

    private void PlayAnimationOnListBoxItem(ListBoxItem listBoxItem, Action onCompleted)
    {
        if (!SystemParameters.ClientAreaAnimation)
        {
            onCompleted();
            return;
        }

        if (Application.Current.TryFindResource("TaskActionAnimation") is Storyboard storyboard)
        {
            try
            {
                var clonedStoryboard = storyboard.Clone();
                clonedStoryboard.Completed += (s, args) =>
                {
                    onCompleted?.Invoke();
                };
                clonedStoryboard.Begin(listBoxItem);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"播放动画失败: {ex.Message}");
                onCompleted?.Invoke();
            }
        }
        else
        {
            onCompleted?.Invoke();
        }
    }

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

    private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is TodoItem todoItem)
        {
            TaskDeleted?.Invoke(this, todoItem);
        }
    }

    private void WidgetView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsWindowInteractionEnabled) return;

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

    private void WidgetView_MouseMove(object sender, MouseEventArgs e)
    {
        if (!IsWindowInteractionEnabled) return;

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
            Cursor = isInResizeArea ? Cursors.SizeNWSE : Cursors.Arrow;
        }
    }

    private void WidgetView_MouseLeftButtonDownForResize(object sender, MouseButtonEventArgs e)
    {
        if (!IsWindowInteractionEnabled) return;

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

    private void WidgetView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // 结束调整大小
        if (_isResizing)
        {
            _isResizing = false;
            Mouse.Capture(null);
        }
    }
}
