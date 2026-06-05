using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.Views;

public partial class WidgetWindow : Window
{
    private readonly WidgetOpacityManager _opacityManager;
    private bool _isDragging = false;
    private bool _positionChangedDuringDrag = false;
    private MainWindow? _mainWindow;

    /// <summary>
    /// 任务勾选事件，直接转发自内部 WidgetContent
    /// </summary>
    public event EventHandler<TodoItem>? TaskChecked
    {
        add => WidgetContent.TaskChecked += value;
        remove => WidgetContent.TaskChecked -= value;
    }

    public WidgetOpacityManager OpacityManager => _opacityManager;

    private const int WM_SIZE = 0x0005;
    private const int SIZE_MINIMIZED = 1;

    private HwndSource? _hwndSource;

    public WidgetWindow()
    {
        InitializeComponent();

        _opacityManager = WidgetOpacityManager.Instance;
        _opacityManager.OpacityChanged += OnOpacityChanged;
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

    private bool _isRestoring = false;

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_SIZE)
        {
            int sizeType = wParam.ToInt32();

            if (sizeType == SIZE_MINIMIZED && !_isRestoring)
            {
                _isRestoring = true;
                MainWindowNativeMethods.ShowWindow(hwnd, MainWindowNativeMethods.SW_RESTORE);
                _isRestoring = false;

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
        var settings = SettingsService.Instance.Settings;
        if (settings.WidgetModeLeft > 0 || settings.WidgetModeTop > 0)
        {
            Left = settings.WidgetModeLeft;
            Top = settings.WidgetModeTop;
            Width = settings.WidgetModeWidth;
            Height = settings.WidgetModeHeight;
        }

        UpdateMousePassThrough();
        UpdateResizeMode();
        UpdateTopmost();
        UpdateImmersionModeMenuItem();

        SettingsService.Instance.SettingsChanged += OnSettingsChanged;
    }

    private void UpdateTopmost()
    {
        var isAlwaysOnTop = SettingsService.Instance.Settings.WidgetAlwaysOnTop;
        Topmost = isAlwaysOnTop;

        var helper = new WindowInteropHelper(this);
        if (helper.Handle == IntPtr.Zero) return;

        int exStyle = MainWindowNativeMethods.GetWindowLong(helper.Handle, MainWindowNativeMethods.GWL_EXSTYLE);

        if (isAlwaysOnTop)
        {
            exStyle &= ~MainWindowNativeMethods.WS_EX_TOOLWINDOW;
            exStyle &= ~MainWindowNativeMethods.WS_EX_NOACTIVATE;
            MainWindowNativeMethods.SetWindowLong(helper.Handle, MainWindowNativeMethods.GWL_EXSTYLE, exStyle);
            MainWindowNativeMethods.SetWindowPos(helper.Handle, IntPtr.Zero, 0, 0, 0, 0,
                MainWindowNativeMethods.SWP_NOMOVE | MainWindowNativeMethods.SWP_NOSIZE | MainWindowNativeMethods.SWP_NOACTIVATE | MainWindowNativeMethods.SWP_FRAMECHANGED);
        }
        else
        {
            exStyle |= MainWindowNativeMethods.WS_EX_TOOLWINDOW;
            exStyle |= MainWindowNativeMethods.WS_EX_NOACTIVATE;
            MainWindowNativeMethods.SetWindowLong(helper.Handle, MainWindowNativeMethods.GWL_EXSTYLE, exStyle);
            MainWindowNativeMethods.SetWindowPos(helper.Handle, MainWindowNativeMethods.HWND_BOTTOM, 0, 0, 0, 0,
                MainWindowNativeMethods.SWP_NOMOVE | MainWindowNativeMethods.SWP_NOSIZE | MainWindowNativeMethods.SWP_NOACTIVATE | MainWindowNativeMethods.SWP_FRAMECHANGED);
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
        // WidgetView 处理视觉透明度，WidgetWindow 仅处理显示/隐藏
        if (effectiveOpacity < 0.01)
        {
            Hide();
        }
        else if (!IsVisible)
        {
            Show();
        }
    }

    private void UpdateMousePassThrough()
    {
        var helper = new WindowInteropHelper(this);
        if (helper.Handle == IntPtr.Zero) return;

        int extendedStyle = MainWindowNativeMethods.GetWindowLong(helper.Handle, MainWindowNativeMethods.GWL_EXSTYLE);

        if (_opacityManager.IsMousePassThroughEnabled)
        {
            MainWindowNativeMethods.SetWindowLong(helper.Handle, MainWindowNativeMethods.GWL_EXSTYLE,
                extendedStyle | MainWindowNativeMethods.WS_EX_TRANSPARENT);
        }
        else
        {
            MainWindowNativeMethods.SetWindowLong(helper.Handle, MainWindowNativeMethods.GWL_EXSTYLE,
                extendedStyle & ~MainWindowNativeMethods.WS_EX_TRANSPARENT);
        }
    }

    private void UpdateResizeMode()
    {
        ResizeMode = _opacityManager.IsMousePassThroughEnabled
            ? ResizeMode.NoResize
            : ResizeMode.CanResizeWithGrip;
    }

    /// <summary>
    /// 设置任务列表，委托给内部 WidgetContent
    /// </summary>
    public void SetTasks(IEnumerable<TodoItem> tasks)
    {
        WidgetContent.SetTasks(tasks);
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
        _opacityManager.PropertyChanged -= OnOpacityManagerPropertyChanged;
        SettingsService.Instance.SettingsChanged -= OnSettingsChanged;
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
            Application.Current.Shutdown();
        });
    }
}
