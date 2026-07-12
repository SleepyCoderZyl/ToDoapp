using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using ToDoapp.Models;
using ToDoapp.Services;

namespace ToDoapp.Views;

public partial class MainWindow
{
    public bool IsWidgetWindowVisible => _widgetWindow?.IsVisible == true;

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
            var useDarkMode = ThemeService.Instance.IsDarkTheme ? 1 : 0;
            var cornerPreference = 2;

            var windowStyle = MainWindowNativeMethods.GetWindowLong(
                helper.Handle,
                MainWindowNativeMethods.GWL_STYLE);
            var normalizedWindowStyle = MainWindowNativeMethods.NormalizeMainWindowStyle(windowStyle);
            MainWindowNativeMethods.SetWindowLong(
                helper.Handle,
                MainWindowNativeMethods.GWL_STYLE,
                normalizedWindowStyle);
            MainWindowNativeMethods.SetWindowPos(
                helper.Handle,
                IntPtr.Zero,
                0,
                0,
                0,
                0,
                MainWindowNativeMethods.SWP_NOSIZE |
                MainWindowNativeMethods.SWP_NOMOVE |
                MainWindowNativeMethods.SWP_NOACTIVATE |
                MainWindowNativeMethods.SWP_FRAMECHANGED);

            var appliedWindowStyle = MainWindowNativeMethods.GetWindowLong(
                helper.Handle,
                MainWindowNativeMethods.GWL_STYLE);
            System.Diagnostics.Debug.WriteLine($"主窗口原生样式: 0x{appliedWindowStyle:X8}");

            MainWindowNativeMethods.DwmSetWindowAttribute(
                helper.Handle,
                darkModeAttribute,
                ref useDarkMode,
                Marshal.SizeOf<int>());

            MainWindowNativeMethods.DwmSetWindowAttribute(
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

    private void WidgetModeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleWidgetMode();
    }

    public void ToggleWidgetMode()
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
        if (_isWidgetMode)
        {
            return;
        }

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
        if (!_isWidgetMode)
        {
            return;
        }

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

        _widgetWindow.SetTasks(GetPendingTodoItems());

        var settings = SettingsService.Instance.Settings;
        var hasSavedWidgetBounds = settings.WidgetModeLeft > 0 || settings.WidgetModeTop > 0;
        if (hasSavedWidgetBounds)
        {
            _widgetWindow.Left = settings.WidgetModeLeft;
            _widgetWindow.Top = settings.WidgetModeTop;
            _widgetWindow.Width = settings.WidgetModeWidth;
            _widgetWindow.Height = settings.WidgetModeHeight;
        }
        else if (_widgetWindowLeft > 0 && _widgetWindowTop > 0)
        {
            _widgetWindow.Left = _widgetWindowLeft;
            _widgetWindow.Top = _widgetWindowTop;
            _widgetWindow.Width = _widgetWindowWidth;
            _widgetWindow.Height = _widgetWindowHeight;
        }
        else
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
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
            try
            {
                if (_widgetWindow.IsLoaded)
                {
                    _widgetWindowLeft = _widgetWindow.Left;
                    _widgetWindowTop = _widgetWindow.Top;
                    _widgetWindowWidth = _widgetWindow.Width;
                    _widgetWindowHeight = _widgetWindow.Height;
                }

                _widgetWindow.TaskChecked -= OnWidgetTaskChecked;
                _widgetWindow.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"关闭小组件窗口失败: {ex.Message}");
            }
            finally
            {
                _widgetWindow = null;
            }
        }

        var helper = new WindowInteropHelper(this);
        if (helper.Handle != IntPtr.Zero)
        {
            var currentStyle = MainWindowNativeMethods.GetWindowLong(helper.Handle, MainWindowNativeMethods.GWL_EXSTYLE);
            currentStyle &= ~MainWindowNativeMethods.WS_EX_TRANSPARENT;
            MainWindowNativeMethods.SetWindowLong(helper.Handle, MainWindowNativeMethods.GWL_EXSTYLE, currentStyle);
        }

        Opacity = 1.0;
        IsHitTestVisible = true;

        RestoreTaskbarIcon();
        Show();
        WindowState = WindowState.Normal;
        Activate();

        UpdateStatus("已切换到主页面");
    }

    public bool ToggleWidgetWindowVisibility()
    {
        if (_widgetWindow == null)
        {
            return false;
        }

        if (_widgetWindow.IsVisible)
        {
            _widgetWindow.Hide();
            return false;
        }

        _widgetWindow.Show();
        _widgetWindow.Activate();
        return true;
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
                SaveData();
            }
        });
    }

    private void HideTaskbarIcon()
    {
        var helper = new WindowInteropHelper(this);
        if (helper.Handle != IntPtr.Zero)
        {
            var currentStyle = MainWindowNativeMethods.GetWindowLong(helper.Handle, MainWindowNativeMethods.GWL_EXSTYLE);
            MainWindowNativeMethods.SetWindowLong(
                helper.Handle,
                MainWindowNativeMethods.GWL_EXSTYLE,
                currentStyle | MainWindowNativeMethods.WS_EX_TOOLWINDOW);
        }

        ShowInTaskbar = false;
    }

    private void RestoreTaskbarIcon()
    {
        var helper = new WindowInteropHelper(this);
        if (helper.Handle != IntPtr.Zero)
        {
            var currentStyle = MainWindowNativeMethods.GetWindowLong(helper.Handle, MainWindowNativeMethods.GWL_EXSTYLE);
            MainWindowNativeMethods.SetWindowLong(
                helper.Handle,
                MainWindowNativeMethods.GWL_EXSTYLE,
                currentStyle & ~MainWindowNativeMethods.WS_EX_TOOLWINDOW);
        }

        ShowInTaskbar = true;
    }

    private void EnableMouseInteraction()
    {
        IsHitTestVisible = true;

        var helper = new WindowInteropHelper(this);
        if (helper.Handle != IntPtr.Zero)
        {
            var currentStyle = MainWindowNativeMethods.GetWindowLong(helper.Handle, MainWindowNativeMethods.GWL_EXSTYLE);
            MainWindowNativeMethods.SetWindowLong(
                helper.Handle,
                MainWindowNativeMethods.GWL_EXSTYLE,
                currentStyle & ~MainWindowNativeMethods.WS_EX_TRANSPARENT);
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
            var currentStyle = MainWindowNativeMethods.GetWindowLong(helper.Handle, MainWindowNativeMethods.GWL_EXSTYLE);
            currentStyle &= ~MainWindowNativeMethods.WS_EX_TRANSPARENT;
            MainWindowNativeMethods.SetWindowLong(
                helper.Handle,
                MainWindowNativeMethods.GWL_EXSTYLE,
                currentStyle | MainWindowNativeMethods.WS_EX_TRANSPARENT);
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
            ExitWidgetMode();
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
            MainWindowNativeMethods.ShowWindow(helper.Handle, MainWindowNativeMethods.SW_RESTORE);
            MainWindowNativeMethods.SetForegroundWindow(helper.Handle);
        }
    }

    private void UpdateWindowFrameState()
    {
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
}
