using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.Views;

public partial class WidgetWindow : Window
{
    private readonly WidgetOpacityManager _opacityManager;
    private SolidColorBrush? _backgroundBrush;
    private SolidColorBrush? _borderBrush;
    private bool _isDragging = false;
    private bool _positionChangedDuringDrag = false;
    private MainWindow? _mainWindow;

    public event EventHandler<TodoItem>? TaskChecked;
    public WidgetOpacityManager OpacityManager => _opacityManager;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private const int WM_SIZE = 0x0005;
    private const int SIZE_MINIMIZED = 1;
    private const int SIZE_RESTORED = 0;

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private HwndSource? _hwndSource;

    public WidgetWindow()
    {
        InitializeComponent();

        _opacityManager = WidgetOpacityManager.Instance;
        _opacityManager.OpacityChanged += OnOpacityChanged;
        _opacityManager.ContentOpacityChanged += OnContentOpacityChanged;
        _opacityManager.PropertyChanged += OnOpacityManagerPropertyChanged;

        Loaded += WidgetWindow_Loaded;
        SourceInitialized += WidgetWindow_SourceInitialized;
    }

    private void WidgetWindow_SourceInitialized(object? sender, EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        _hwndSource = HwndSource.FromHwnd(helper.Handle);
        _hwndSource?.AddHook(WndProc);
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;
    private const int SW_SHOWNA = 8;

    private bool _isRestoring = false;

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_SIZE)
        {
            int sizeType = wParam.ToInt32();
            
            if (sizeType == SIZE_MINIMIZED && !_isRestoring)
            {
                _isRestoring = true;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ShowWindow(hwnd, SW_RESTORE);
                    _isRestoring = false;
                }), System.Windows.Threading.DispatcherPriority.Normal);
                
                handled = true;
                return IntPtr.Zero;
            }
        }
        return IntPtr.Zero;
    }

    public void SetMainWindow(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    private void WidgetWindow_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshThemeBrushes();

        var settings = SettingsService.Instance.Settings;
        if (settings.WidgetModeLeft > 0 || settings.WidgetModeTop > 0)
        {
            Left = settings.WidgetModeLeft;
            Top = settings.WidgetModeTop;
            Width = settings.WidgetModeWidth;
            Height = settings.WidgetModeHeight;
        }

        UpdateOpacity();
        UpdateContentOpacity();
        UpdateMousePassThrough();
        UpdateResizeMode();
        UpdateTopmost();
        UpdateImmersionModeMenuItem();

        SettingsService.Instance.SettingsChanged += OnSettingsChanged;
    }

    public void RefreshThemeBrushes()
    {
        _backgroundBrush = new SolidColorBrush(GetResourceColor("WidgetBackgroundBrush", Color.FromRgb(32, 32, 32)));
        BackgroundBorder.Background = _backgroundBrush;

        _borderBrush = new SolidColorBrush(GetResourceColor("BorderBrush", Color.FromRgb(61, 61, 61)));
        BackgroundBorder.BorderBrush = _borderBrush;

        UpdateOpacity();
    }

    private static Color GetResourceColor(string key, Color fallback)
    {
        return Application.Current.TryFindResource(key) is SolidColorBrush brush
            ? brush.Color
            : fallback;
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;

    private void UpdateTopmost()
    {
        var isAlwaysOnTop = SettingsService.Instance.Settings.WidgetAlwaysOnTop;
        Topmost = isAlwaysOnTop;

        var helper = new WindowInteropHelper(this);
        if (helper.Handle == IntPtr.Zero) return;

        int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);

        if (isAlwaysOnTop)
        {
            exStyle &= ~WS_EX_TOOLWINDOW;
            exStyle &= ~WS_EX_NOACTIVATE;
            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle);
            SetWindowPos(helper.Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        }
        else
        {
            exStyle |= WS_EX_TOOLWINDOW;
            exStyle |= WS_EX_NOACTIVATE;
            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle);
            SetWindowPos(helper.Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        }
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        UpdateTopmost();
    }

    private void OnOpacityManagerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WidgetOpacityManager.IsMousePassThroughEnabled))
        {
            UpdateMousePassThrough();
            UpdateResizeMode();
            UpdateImmersionModeMenuItem();
        }
    }

    private void UpdateImmersionModeMenuItem()
    {
        if (ImmersionModeMenuItem != null)
        {
            ImmersionModeMenuItem.Header = _opacityManager.IsMousePassThroughEnabled ? "退出沉浸模式" : "进入沉浸模式";
        }
    }

    private void OnOpacityChanged(object? sender, double effectiveOpacity)
    {
        UpdateOpacity();
    }

    private void OnContentOpacityChanged(object? sender, double effectiveContentOpacity)
    {
        UpdateContentOpacity();
    }

    private void UpdateOpacity()
    {
        var opacity = _opacityManager.IsMousePassThroughEnabled
            ? _opacityManager.WidgetOpacity
            : 1.0;

        if (_backgroundBrush != null)
        {
            var color = _backgroundBrush.Color;
            color.A = (byte)(255 * opacity);
            _backgroundBrush.Color = color;
        }

        if (_borderBrush != null)
        {
            var borderColor = _borderBrush.Color;
            borderColor.A = (byte)(255 * opacity);
            _borderBrush.Color = borderColor;
        }

        if (opacity < 0.01)
        {
            Hide();
        }
        else if (!IsVisible)
        {
            Show();
        }
    }

    private void UpdateContentOpacity()
    {
        var contentOpacity = _opacityManager.IsMousePassThroughEnabled
            ? _opacityManager.EffectiveContentOpacity
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

    private void UpdateMousePassThrough()
    {
        var helper = new WindowInteropHelper(this);
        if (helper.Handle == IntPtr.Zero) return;

        int extendedStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);

        if (_opacityManager.IsMousePassThroughEnabled)
        {
            SetWindowLong(helper.Handle, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }
        else
        {
            SetWindowLong(helper.Handle, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
        }
    }

    private void UpdateResizeMode()
    {
        ResizeMode = _opacityManager.IsMousePassThroughEnabled
            ? ResizeMode.NoResize
            : ResizeMode.CanResizeWithGrip;
    }

    public void SetTasks(IEnumerable<TodoItem> tasks)
    {
        if (WidgetTasksListBox != null)
        {
            WidgetTasksListBox.ItemsSource = tasks;
        }

        var taskList = new List<TodoItem>(tasks);
        if (EmptyMessage != null)
        {
            EmptyMessage.Visibility = taskList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void TaskCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is TodoItem todoItem)
        {
            // 获取 ListBoxItem 容器并播放动画
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
            catch
            {
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

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        base.OnMouseLeftButtonDown(e);

        var clickedElement = e.OriginalSource as DependencyObject;
        while (clickedElement != null && !(clickedElement is Button) && !(clickedElement is CheckBox))
        {
            clickedElement = VisualTreeHelper.GetParent(clickedElement);
        }

        if (clickedElement is Button || clickedElement is CheckBox)
        {
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _isDragging = true;
            _positionChangedDuringDrag = false;
            try
            {
                DragMove();
            }
            finally
            {
                _isDragging = false;
                if (_positionChangedDuringDrag)
                {
                    SaveWindowPosition();
                }
            }
        }
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        if (_isDragging)
        {
            _positionChangedDuringDrag = true;
        }
        else
        {
            SaveWindowPosition();
        }
    }

    private void SaveWindowPosition()
    {
        SettingsService.Instance.UpdateWidgetModePosition(Width, Height, Left, Top);
    }

    protected override void OnClosed(EventArgs e)
    {
        _hwndSource?.RemoveHook(WndProc);
        _opacityManager.OpacityChanged -= OnOpacityChanged;
        _opacityManager.ContentOpacityChanged -= OnContentOpacityChanged;
        _opacityManager.PropertyChanged -= OnOpacityManagerPropertyChanged;
        base.OnClosed(e);
    }

    private void ToggleMainWindow_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_mainWindow != null)
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        _mainWindow.ToggleWidgetMode();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"切换小组件模式失败: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ToggleMainWindow_Click 异常: {ex.Message}");
        }
    }

    private void ToggleImmersionMode_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_mainWindow != null)
            {
                _mainWindow.ToggleMousePassThrough();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ToggleImmersionMode_Click 异常: {ex.Message}");
        }
    }

    private void ShowSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_mainWindow != null)
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var settingsWindow = new SettingsWindow
                        {
                            Owner = _mainWindow
                        };
                        settingsWindow.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"打开设置窗口失败: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ShowSettings_Click 异常: {ex.Message}");
        }
    }

    private void ExitApplication_Click(object sender, RoutedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            System.Windows.Application.Current.Shutdown();
        });
    }
}
