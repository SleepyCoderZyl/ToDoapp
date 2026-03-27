using System;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Drawing;
using ToDoapp;
using ToDoapp.Views;

namespace ToDoapp.Services;

/// <summary>
/// 系统托盘服务，提供完整的托盘图标和菜单功能
/// </summary>
public class SystemTrayService : IDisposable
{
    private MainWindow _mainWindow;
    private NotifyIconData _notifyIconData;
    private IntPtr _windowHandle;
    private bool _isDisposed;
    private bool _isInitialized;

    // 消息常量
    private const int WM_USER = 0x0400;
    private const int WM_TRAYICON = WM_USER + 1;
    private const int WM_TRAYICON_MESSAGE = WM_USER + 2;

    // 托盘图标消息
    private const int NIM_ADD = 0x00000000;
    private const int NIM_MODIFY = 0x00000001;
    private const int NIM_DELETE = 0x00000002;
    private const int NIM_SETVERSION = 0x00000004;

    // 托盘图标事件
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_RBUTTONDBLCLK = 0x0206;

    public SystemTrayService(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _isInitialized = false;
        
        // 监听窗口的SourceInitialized事件，确保窗口句柄已创建
        _mainWindow.SourceInitialized += MainWindow_SourceInitialized;
    }

    /// <summary>
    /// 窗口初始化完成事件处理
    /// </summary>
    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        try
        {
            _windowHandle = new WindowInteropHelper(_mainWindow).Handle;
            if (_windowHandle != IntPtr.Zero)
            {
                InitializeNotifyIcon();
                RegisterWindowMessageHandler();
                _isInitialized = true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"初始化系统托盘失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 初始化系统托盘图标
    /// </summary>
    private void InitializeNotifyIcon()
    {
        try
        {
            _notifyIconData = new NotifyIconData
            {
                cbSize = Marshal.SizeOf(typeof(NotifyIconData)),
                hWnd = _windowHandle,
                uID = 1,
                uFlags = NIF_ICON | NIF_TIP | NIF_MESSAGE,
                uCallbackMessage = WM_TRAYICON,
                hIcon = LoadDefaultIcon(),
                szTip = "待办便签\0"
            };

            Shell_NotifyIcon(NIM_ADD, ref _notifyIconData);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"初始化托盘图标失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载默认图标
    /// </summary>
    private IntPtr LoadDefaultIcon()
    {
        try
        {
            string iconPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "待办_16x16.ico");
            if (System.IO.File.Exists(iconPath))
            {
                var icon = new Icon(iconPath);
                return icon.Handle;
            }
            return LoadIcon(IntPtr.Zero, IDI_APPLICATION);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载图标失败: {ex.Message}");
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// 注册窗口消息处理器
    /// </summary>
    private void RegisterWindowMessageHandler()
    {
        try
        {
            var source = HwndSource.FromHwnd(_windowHandle);
            source?.AddHook(WndProc);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"注册消息处理器失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 窗口消息处理
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_TRAYICON)
        {
            int mouseMessage = lParam.ToInt32();
            switch (mouseMessage)
            {
                case WM_LBUTTONDBLCLK:
                    ShowWindow_Click();
                    handled = true;
                    break;
                case WM_RBUTTONUP:
                    ShowContextMenu();
                    handled = true;
                    break;
            }
        }
        return IntPtr.Zero;
    }

    /// <summary>
    /// 显示窗口（根据当前模式显示主窗口或小组件）
    /// </summary>
    private void ShowWindow_Click()
    {
        try
        {
            var method = _mainWindow.GetType().GetMethod("IsWidgetMode", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            bool isWidgetMode = method != null && (bool)method.Invoke(_mainWindow, null)!;
            
            if (isWidgetMode)
            {
                ToggleWidgetVisibility();
            }
            else
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
                _mainWindow.Focus();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示窗口失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 切换小组件的显示/隐藏状态
    /// </summary>
    private void ToggleWidgetVisibility()
    {
        try
        {
            var field = _mainWindow.GetType().GetField("_widgetWindow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var widgetWindow = field?.GetValue(_mainWindow) as Window;
            
            if (widgetWindow != null)
            {
                if (widgetWindow.IsVisible)
                {
                    widgetWindow.Hide();
                    ShowNotification("待办便签", "小组件已隐藏");
                }
                else
                {
                    widgetWindow.Show();
                    widgetWindow.Activate();
                    ShowNotification("待办便签", "小组件已显示");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"切换小组件显示状态失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示上下文菜单
    /// </summary>
    private void ShowContextMenu()
    {
        try
        {
            // 获取当前鼠标位置
            POINT mousePos;
            GetCursorPos(out mousePos);
            
            // 创建菜单
            IntPtr hMenu = CreatePopupMenu();
            
            // 获取当前小组件模式状态
            bool isWidgetMode = false;
            // 获取小组件窗口可见状态
            bool isWidgetWindowVisible = false;
            // 获取当前鼠标穿透状态
            bool isMousePassThroughEnabled = false;
            
            _mainWindow.Dispatcher.Invoke(() =>
            {
                // 通过反射调用主窗口的IsWidgetMode方法
                var method2 = _mainWindow.GetType().GetMethod("IsWidgetMode", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (method2 != null)
                {
                    isWidgetMode = (bool)method2.Invoke(_mainWindow, null)!;
                }
                
                // 获取小组件窗口可见状态和鼠标穿透状态（仅在小组件模式下）
                if (isWidgetMode)
                {
                    var field = _mainWindow.GetType().GetField("_widgetWindow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var widgetWindow = field?.GetValue(_mainWindow) as Window;
                    isWidgetWindowVisible = widgetWindow?.IsVisible ?? false;
                    
                    // 通过反射调用主窗口的IsMousePassThroughEnabled方法
                    var method1 = _mainWindow.GetType().GetMethod("IsMousePassThroughEnabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (method1 != null)
                    {
                        isMousePassThroughEnabled = (bool)method1.Invoke(_mainWindow, null)!;
                    }
                }
            });
            
            // 添加菜单项
            AppendMenu(hMenu, MF_STRING, 3, isWidgetMode ? "切换到主页面" : "切换到小组件");
            
            if (isWidgetMode)
            {
                AppendMenu(hMenu, MF_STRING, 7, isWidgetWindowVisible ? "隐藏小组件" : "显示小组件");
                AppendMenu(hMenu, MF_STRING, 4, isMousePassThroughEnabled ? "退出沉浸模式" : "进入沉浸模式");
            }
            
            AppendMenu(hMenu, MF_STRING, 6, "设置");
            AppendMenu(hMenu, MF_STRING, 8, "关于");
            AppendMenu(hMenu, MF_SEPARATOR, 0, "");
            AppendMenu(hMenu, MF_STRING, 5, "退出程序");
            
            // 显示菜单
            SetForegroundWindow(_windowHandle);
            int result = TrackPopupMenu(hMenu, TPM_LEFTALIGN | TPM_TOPALIGN | TPM_RETURNCMD, mousePos.X, mousePos.Y, 0, _windowHandle, IntPtr.Zero);
            
            // 处理菜单选择
            switch (result)
            {
                case 3:
                    ToggleWidgetMode_Click();
                    break;
                case 4:
                    ToggleMousePassThrough_Click();
                    break;
                case 5:
                    Exit_Click();
                    break;
                case 6:
                    Settings_Click();
                    break;
                case 7:
                    ToggleWidgetVisibility();
                    break;
                case 8:
                    About_Click();
                    break;
            }
            
            // 销毁菜单
            DestroyMenu(hMenu);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示上下文菜单失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建Win11风格的菜单项
    /// </summary>
    private System.Windows.Controls.MenuItem CreateMenuItem(string header, System.Windows.RoutedEventHandler clickHandler)
    {
        var menuItem = new System.Windows.Controls.MenuItem { Header = header };
        menuItem.Click += clickHandler;
        
        // 设置菜单项样式
        menuItem.Background = System.Windows.Media.Brushes.Transparent;
        menuItem.Foreground = System.Windows.Media.Brushes.White;
        menuItem.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
        menuItem.FontSize = 12;
        menuItem.Padding = new System.Windows.Thickness(8, 4, 8, 4);
        
        // 添加鼠标悬停效果
        menuItem.Style = new System.Windows.Style(typeof(System.Windows.Controls.MenuItem));
        menuItem.Style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.BackgroundProperty, System.Windows.Media.Brushes.Transparent));
        menuItem.Style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.ForegroundProperty, System.Windows.Media.Brushes.White));
        
        // 添加触发器
        var mouseOverTrigger = new System.Windows.Trigger { Property = System.Windows.UIElement.IsMouseOverProperty, Value = true };
        mouseOverTrigger.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.BackgroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 44, 44, 44))));
        menuItem.Style.Triggers.Add(mouseOverTrigger);
        
        return menuItem;
    }

    /// <summary>
    /// 切换小组件模式
    /// </summary>
    private void ToggleWidgetMode_Click()
    {
        try
        {
            _mainWindow.Dispatcher.Invoke(() =>
            {
                var method = _mainWindow.GetType().GetMethod("ToggleWidgetMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(_mainWindow, null);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"切换小组件模式失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 切换鼠标穿透状态
    /// </summary>
    private void ToggleMousePassThrough_Click()
    {
        try
        {
            _mainWindow.Dispatcher.Invoke(() =>
            {
                _mainWindow.ToggleMousePassThrough();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"切换鼠标穿透失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 打开设置窗口
    /// </summary>
    private void Settings_Click()
    {
        try
        {
            _mainWindow.Dispatcher.Invoke(() =>
            {
                var settingsWindow = new SettingsWindow
                {
                    Owner = _mainWindow
                };
                settingsWindow.ShowDialog();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"打开设置窗口失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示关于对话框
    /// </summary>
    private void About_Click()
    {
        try
        {
            _mainWindow.Dispatcher.Invoke(() =>
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetName().Version?.ToString() ?? "1.1.0";
                
                var aboutPanel = new System.Windows.Controls.StackPanel
                {
                    Margin = new Thickness(20)
                };

                var appName = new System.Windows.Controls.TextBlock
                {
                    Text = "待办便签",
                    FontSize = 24,
                    FontWeight = System.Windows.FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                aboutPanel.Children.Add(appName);

                var versionText = new System.Windows.Controls.TextBlock
                {
                    Text = $"版本 {version}",
                    FontSize = 14,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                aboutPanel.Children.Add(versionText);

                var descText = new System.Windows.Controls.TextBlock
                {
                    Text = "一个简洁的待办事项管理工具",
                    FontSize = 13,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 213, 219)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                aboutPanel.Children.Add(descText);

                var separator = new System.Windows.Controls.Separator
                {
                    Margin = new Thickness(0, 10, 0, 15),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(61, 61, 61))
                };
                aboutPanel.Children.Add(separator);

                var authorText = new System.Windows.Controls.TextBlock
                {
                    Text = "作者: zylin",
                    FontSize = 13,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                aboutPanel.Children.Add(authorText);

                var techText = new System.Windows.Controls.TextBlock
                {
                    Text = "技术栈: C# / WPF / .NET 10",
                    FontSize = 12,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                aboutPanel.Children.Add(techText);

                DialogService.ShowCustomDialog("关于", DialogType.None, aboutPanel, "确定");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示关于对话框失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 完全退出应用
    /// </summary>
    private void Exit_Click()
    {
        try
        {
            Dispose();
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"退出应用失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 最小化到托盘
    /// </summary>
    public void MinimizeToTray()
    {
        try
        {
            _mainWindow.Hide();
            ShowNotification("待办便签", "应用已最小化到系统托盘");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"最小化到托盘失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示托盘通知
    /// </summary>
    public void ShowNotification(string title, string message)
    {
        try
        {
            if (_isInitialized && _windowHandle != IntPtr.Zero)
            {
                _notifyIconData.uFlags = NIF_INFO;
                _notifyIconData.szInfo = message + "\0";
                _notifyIconData.szInfoTitle = title + "\0";
                _notifyIconData.dwInfoFlags = NIIF_INFO;
                
                Shell_NotifyIcon(NIM_MODIFY, ref _notifyIconData);
                
                // 恢复原始标志
                _notifyIconData.uFlags = NIF_ICON | NIF_TIP | NIF_MESSAGE;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示通知失败: {ex.Message}");
        }
    }

    public void Dispose()
    {
        try
        {
            if (!_isDisposed && _isInitialized && _windowHandle != IntPtr.Zero)
            {
                Shell_NotifyIcon(NIM_DELETE, ref _notifyIconData);
                _isDisposed = true;
            }
            GC.SuppressFinalize(this);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"释放资源失败: {ex.Message}");
        }
    }

    // 常量定义
    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;
    private const int NIF_MESSAGE = 0x00000001;
    private const int NIF_INFO = 0x00000010;
    private const int NIIF_INFO = 0x00000001;
    private const int IDI_APPLICATION = 32512;
    
    // 菜单常量
    private const int MF_STRING = 0x00000000;
    private const int MF_SEPARATOR = 0x00000800;
    private const int TPM_LEFTALIGN = 0x00000000;
    private const int TPM_TOPALIGN = 0x00000000;
    private const int TPM_RETURNCMD = 0x00000100;

    // Windows API 函数
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NotifyIconData pnid);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr LoadIcon(IntPtr hInstance, int lpIconName);
    
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);
    
    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();
    
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIDNewItem, string lpNewItem);
    
    [DllImport("user32.dll")]
    private static extern int TrackPopupMenu(IntPtr hMenu, int uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr lprc);
    
    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);
    
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    
    // 结构体
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    // 托盘图标数据结构
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct NotifyIconData
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }
}