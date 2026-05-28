using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using ToDoapp.Constants;
using ToDoapp.Views;

namespace ToDoapp.Services;

public class SystemTrayService : IDisposable
{
    private readonly ITrayActionHandler _actionHandler;
    private readonly Window _hostWindow;
    private NotifyIconData _notifyIconData;
    private Icon? _trayIcon;
    private IntPtr _windowHandle;
    private bool _isDisposed;
    private bool _isInitialized;
    private uint _taskbarCreatedMessage;
    private readonly UpdateService _updateService = new();

    private const int WM_USER = 0x0400;
    private const int WM_TRAYICON = WM_USER + 1;
    private const int WM_TRAYICON_MESSAGE = WM_USER + 2;

    private const int NIM_ADD = 0x00000000;
    private const int NIM_MODIFY = 0x00000001;
    private const int NIM_DELETE = 0x00000002;
    private const int NIM_SETVERSION = 0x00000004;

    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_RBUTTONDBLCLK = 0x0206;

    public SystemTrayService(ITrayActionHandler actionHandler)
    {
        _actionHandler = actionHandler;
        _hostWindow = actionHandler.TrayHostWindow;
        _isInitialized = false;
        _hostWindow.SourceInitialized += MainWindow_SourceInitialized;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        try
        {
            _windowHandle = new WindowInteropHelper(_hostWindow).Handle;
            if (_windowHandle != IntPtr.Zero)
            {
                _taskbarCreatedMessage = RegisterWindowMessage("TaskbarCreated");
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

    private void RecreateNotifyIcon()
    {
        try
        {
            if (_isDisposed || _windowHandle == IntPtr.Zero)
            {
                return;
            }

            _notifyIconData.hIcon = LoadDefaultIcon();
            Shell_NotifyIcon(NIM_ADD, ref _notifyIconData);
            System.Diagnostics.Debug.WriteLine("托盘图标已重新创建");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"重新创建托盘图标失败: {ex.Message}");
        }
    }

    private IntPtr LoadDefaultIcon()
    {
        try
        {
            string iconPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "待办_16x16.ico");
            if (System.IO.File.Exists(iconPath))
            {
                _trayIcon?.Dispose();
                using var fileIcon = new Icon(iconPath);
                _trayIcon = (Icon)fileIcon.Clone();
                return _trayIcon.Handle;
            }

            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(processPath) && System.IO.File.Exists(processPath))
            {
                using var extractedIcon = Icon.ExtractAssociatedIcon(processPath);
                if (extractedIcon != null)
                {
                    _trayIcon?.Dispose();
                    _trayIcon = (Icon)extractedIcon.Clone();
                    return _trayIcon.Handle;
                }
            }

            return LoadIcon(IntPtr.Zero, IDI_APPLICATION);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载图标失败: {ex.Message}");
            return IntPtr.Zero;
        }
    }

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

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == _taskbarCreatedMessage && _taskbarCreatedMessage != 0)
        {
            RecreateNotifyIcon();
            handled = true;
            return IntPtr.Zero;
        }

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

    private void ShowWindow_Click()
    {
        try
        {
            if (_actionHandler.IsWidgetMode())
            {
                ToggleWidgetVisibility();
            }
            else
            {
                _actionHandler.RestoreMainWindow();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示窗口失败: {ex.Message}");
        }
    }

    private void ToggleWidgetVisibility()
    {
        try
        {
            var isVisible = _actionHandler.ToggleWidgetWindowVisibility();
            if (_actionHandler.IsWidgetMode())
            {
                ShowNotification("待办便签", isVisible ? "小组件已显示" : "小组件已隐藏");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"切换小组件显示状态失败: {ex.Message}");
        }
    }

    private void ShowContextMenu()
    {
        try
        {
            POINT mousePos;
            GetCursorPos(out mousePos);

            IntPtr hMenu = CreatePopupMenu();
            IntPtr hImportExportMenu = CreatePopupMenu();

            bool isWidgetMode = false;
            bool isWidgetWindowVisible = false;
            bool isMousePassThroughEnabled = false;

            _hostWindow.Dispatcher.Invoke(() =>
            {
                isWidgetMode = _actionHandler.IsWidgetMode();

                if (isWidgetMode)
                {
                    isWidgetWindowVisible = _actionHandler.IsWidgetWindowVisible;
                    isMousePassThroughEnabled = _actionHandler.IsMousePassThroughEnabled();
                }
            });

            AppendMenu(hMenu, MF_STRING, MenuToggleWidgetMode, isWidgetMode ? "切换到主页面" : "切换到小组件");

            if (isWidgetMode)
            {
                AppendMenu(hMenu, MF_STRING, MenuToggleWidgetVisibility, isWidgetWindowVisible ? "隐藏小组件" : "显示小组件");
                AppendMenu(hMenu, MF_STRING, MenuToggleImmersionMode, isMousePassThroughEnabled ? "退出沉浸模式" : "进入沉浸模式");
            }

            AppendMenu(hImportExportMenu, MF_STRING, MenuImportJson, "导入 JSON 文件");
            AppendMenu(hImportExportMenu, MF_STRING, MenuExportJson, "导出 JSON 文件");
            AppendMenu(hImportExportMenu, MF_STRING, MenuRestoreBackup, "恢复备份");
            AppendMenu(hMenu, MF_POPUP, hImportExportMenu, "导入/导出");

            AppendMenu(hMenu, MF_STRING, MenuSettings, "设置");
            AppendMenu(hMenu, MF_STRING, MenuHelp, "帮助");
            AppendMenu(hMenu, MF_STRING, MenuAbout, "关于");
            AppendMenu(hMenu, MF_SEPARATOR, 0, "");
            AppendMenu(hMenu, MF_STRING, MenuExit, "退出程序");

            SetForegroundWindow(_windowHandle);
            int result = TrackPopupMenu(hMenu, TPM_LEFTALIGN | TPM_TOPALIGN | TPM_RETURNCMD, mousePos.X, mousePos.Y, 0, _windowHandle, IntPtr.Zero);

            switch (result)
            {
                case MenuToggleWidgetMode:
                    ToggleWidgetMode_Click();
                    break;
                case MenuToggleImmersionMode:
                    ToggleMousePassThrough_Click();
                    break;
                case MenuExit:
                    Exit_Click();
                    break;
                case MenuSettings:
                    Settings_Click();
                    break;
                case MenuHelp:
                    Help_Click();
                    break;
                case MenuToggleWidgetVisibility:
                    ToggleWidgetVisibility();
                    break;
                case MenuAbout:
                    About_Click();
                    break;
                case MenuImportJson:
                    ImportJson_Click();
                    break;
                case MenuExportJson:
                    ExportJson_Click();
                    break;
                case MenuRestoreBackup:
                    RestoreBackup_Click();
                    break;
            }

            DestroyMenu(hImportExportMenu);
            DestroyMenu(hMenu);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示上下文菜单失败: {ex.Message}");
        }
    }

    private System.Windows.Controls.MenuItem CreateMenuItem(string header, System.Windows.RoutedEventHandler clickHandler)
    {
        var menuItem = new System.Windows.Controls.MenuItem { Header = header };
        menuItem.Click += clickHandler;

        menuItem.Background = System.Windows.Media.Brushes.Transparent;
        menuItem.Foreground = System.Windows.Media.Brushes.White;
        menuItem.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
        menuItem.FontSize = 12;
        menuItem.Padding = new System.Windows.Thickness(8, 4, 8, 4);

        menuItem.Style = new System.Windows.Style(typeof(System.Windows.Controls.MenuItem));
        menuItem.Style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.BackgroundProperty, System.Windows.Media.Brushes.Transparent));
        menuItem.Style.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.ForegroundProperty, System.Windows.Media.Brushes.White));

        var mouseOverTrigger = new System.Windows.Trigger { Property = System.Windows.UIElement.IsMouseOverProperty, Value = true };
        mouseOverTrigger.Setters.Add(new System.Windows.Setter(System.Windows.Controls.Control.BackgroundProperty, new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 44, 44, 44))));
        menuItem.Style.Triggers.Add(mouseOverTrigger);

        return menuItem;
    }

    private void ToggleWidgetMode_Click()
    {
        try
        {
            _hostWindow.Dispatcher.Invoke(() =>
            {
                _actionHandler.ToggleWidgetMode();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"切换小组件模式失败: {ex.Message}");
        }
    }

    private void ToggleMousePassThrough_Click()
    {
        try
        {
            _hostWindow.Dispatcher.Invoke(() =>
            {
                _actionHandler.ToggleMousePassThrough();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"切换鼠标穿透失败: {ex.Message}");
        }
    }

    private void Settings_Click()
    {
        try
        {
            _hostWindow.Dispatcher.Invoke(() =>
            {
                _actionHandler.ShowSettingsWindow();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"打开设置窗口失败: {ex.Message}");
        }
    }

    private void Help_Click()
    {
        try
        {
            var helpPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help.html");
            if (!System.IO.File.Exists(helpPath))
            {
                HandyControl.Controls.MessageBox.Error($"未找到帮助文件：{helpPath}", "打开帮助失败");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = helpPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"打开帮助页面失败: {ex.Message}");
            HandyControl.Controls.MessageBox.Error($"无法打开帮助页面：{ex.Message}", "打开帮助失败");
        }
    }

    private void ImportJson_Click()
    {
        try
        {
            _hostWindow.Dispatcher.Invoke(() =>
            {
                _actionHandler.ImportTodosFromJsonFile();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"导入 JSON 文件失败: {ex.Message}");
        }
    }

    private void ExportJson_Click()
    {
        try
        {
            _hostWindow.Dispatcher.Invoke(() =>
            {
                _actionHandler.ExportTodosToJsonFile();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"导出 JSON 文件失败: {ex.Message}");
        }
    }

    private void RestoreBackup_Click()
    {
        try
        {
            _hostWindow.Dispatcher.Invoke(() =>
            {
                _actionHandler.ShowBackupRecoveryDialog();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"打开备份恢复失败: {ex.Message}");
        }
    }

    private void About_Click()
    {
        try
        {
            _hostWindow.Dispatcher.Invoke(() =>
            {
                var currentVersion = _updateService.GetCurrentVersion();
                var statusText = CreateAboutStatusText("点击检查更新获取最新版本。", GetResourceBrush("TextSecondaryBrush"));
                var detailText = CreateAboutStatusText(string.Empty, GetResourceBrush("TextMutedBrush"));
                detailText.Visibility = Visibility.Collapsed;

                var checkUpdateButton = CreateActionButton("检查更新", "DialogButtonStyle");
                var openDownloadButton = CreateActionButton("前往下载", "DialogButtonStyle");
                openDownloadButton.Visibility = Visibility.Collapsed;

                checkUpdateButton.Click += async (_, _) =>
                {
                    checkUpdateButton.IsEnabled = false;
                    openDownloadButton.Visibility = Visibility.Collapsed;
                    statusText.Text = "正在检查更新…";
                    statusText.Foreground = GetResourceBrush("TextPrimaryBrush");
                    detailText.Text = "正在连接 GitHub Releases 获取最新版本信息。";
                    detailText.Visibility = Visibility.Visible;

                    try
                    {
                        var result = await _updateService.CheckForUpdatesAsync();
                        statusText.Text = result.StatusText;
                        statusText.Foreground = result.IsSuccess
                            ? GetResourceBrush("TextPrimaryBrush")
                            : GetResourceBrush("DangerBrush");

                        detailText.Text = result.DetailText;
                        detailText.Visibility = string.IsNullOrWhiteSpace(result.DetailText)
                            ? Visibility.Collapsed
                            : Visibility.Visible;

                        if (result.IsSuccess && result.HasUpdate && !string.IsNullOrWhiteSpace(result.DownloadUrl))
                        {
                            openDownloadButton.Tag = result.DownloadUrl;
                            openDownloadButton.Visibility = Visibility.Visible;
                        }
                    }
                    catch (Exception ex)
                    {
                        statusText.Text = "检查更新失败";
                        statusText.Foreground = GetResourceBrush("DangerBrush");
                        detailText.Text = $"发生未预期错误：{ex.Message}";
                        detailText.Visibility = Visibility.Visible;
                    }
                    finally
                    {
                        checkUpdateButton.IsEnabled = true;
                    }
                };

                openDownloadButton.Click += (_, _) =>
                {
                    try
                    {
                        var targetUrl = openDownloadButton.Tag as string;
                        if (string.IsNullOrWhiteSpace(targetUrl))
                        {
                            targetUrl = AppConstants.UpdateDownloadUrl;
                        }

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = targetUrl,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        HandyControl.Controls.MessageBox.Error($"无法打开下载页面：{ex.Message}", "打开失败");
                    }
                };

                var aboutPanel = BuildAboutPanel(currentVersion, statusText, detailText, checkUpdateButton, openDownloadButton);
                DialogService.ShowCustomDialog(
                    "关于",
                    DialogType.None,
                    aboutPanel,
                    "关闭",
                    null,
                    (primaryButton, secondaryButton) =>
                    {
                        primaryButton.Visibility = Visibility.Collapsed;
                        secondaryButton.Visibility = Visibility.Collapsed;
                    },
                    showTitleCloseButton: true,
                    dialogWidth: 480);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示关于对话框失败: {ex.Message}");
        }
    }

    private FrameworkElement BuildAboutPanel(
        string currentVersion,
        TextBlock statusText,
        TextBlock detailText,
        Button checkUpdateButton,
        Button openDownloadButton)
    {
        var panel = new StackPanel
        {
            Width = 420,
            MaxWidth = 420,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        panel.Children.Add(new TextBlock
        {
            Text = "待办便签",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = GetResourceBrush("TextPrimaryBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"版本 {currentVersion}",
            FontSize = 13,
            Foreground = GetResourceBrush("TextSecondaryBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0)
        });
        panel.Children.Add(new TextBlock
        {
            Text = "一个简洁的待办事项管理工具",
            FontSize = 13,
            Foreground = GetResourceBrush("TextSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 10, 0, 12)
        });

        panel.Children.Add(CreateAboutSeparator());

        var updateTitle = new TextBlock
        {
            Text = "检查更新",
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = GetResourceBrush("TextPrimaryBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 8)
        };
        panel.Children.Add(updateTitle);
        panel.Children.Add(checkUpdateButton);
        panel.Children.Add(statusText);
        panel.Children.Add(detailText);

        openDownloadButton.HorizontalAlignment = HorizontalAlignment.Center;
        openDownloadButton.Margin = new Thickness(0, 8, 0, 0);
        panel.Children.Add(openDownloadButton);

        panel.Children.Add(CreateAboutSeparator(new Thickness(0, 12, 0, 12)));
        panel.Children.Add(CreateInfoRow("产品名称", "待办便签"));
        panel.Children.Add(CreateInfoRow("技术栈", "C# / WPF / .NET 10"));
        panel.Children.Add(CreateInfoRow("更新来源", "GitHub Releases"));
        panel.Children.Add(CreateInfoRow("作者", "zylin", false));

        return panel;
    }

    private Border CreateAboutSeparator(Thickness? margin = null)
    {
        return new Border
        {
            Height = 1,
            Background = GetResourceBrush("BackgroundLightBrush"),
            Margin = margin ?? new Thickness(0, 0, 0, 14)
        };
    }

    private Grid CreateInfoRow(string labelText, string valueText, bool hasBottomMargin = true)
    {
        var row = new Grid
        {
            Margin = hasBottomMargin ? new Thickness(0, 0, 0, 8) : new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = 280
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var label = new TextBlock
        {
            Text = labelText,
            FontSize = 12,
            Foreground = GetResourceBrush("TextMutedBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        var value = new TextBlock
        {
            Text = valueText,
            FontSize = 12,
            Foreground = GetResourceBrush("TextSecondaryBrush"),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetColumn(label, 0);
        Grid.SetColumn(value, 1);
        row.Children.Add(label);
        row.Children.Add(value);
        return row;
    }

    private TextBlock CreateAboutStatusText(string text, System.Windows.Media.Brush foreground)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 13,
            Foreground = foreground,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0),
            MaxWidth = 320
        };
    }

    private Button CreateActionButton(string text, string styleKey)
    {
        return new Button
        {
            Content = text,
            Style = Application.Current.Resources[styleKey] as Style,
            MinWidth = 110,
            Margin = new Thickness(0),
            Padding = new Thickness(16, 8, 16, 8),
            Foreground = GetResourceBrush("TextPrimaryBrush"),
            HorizontalAlignment = HorizontalAlignment.Center
        };
    }

    private System.Windows.Media.Brush GetResourceBrush(string key)
    {
        return Application.Current.Resources[key] as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.White;
    }

    private void Exit_Click()
    {
        try
        {
            Dispose();
            _actionHandler.ExitApplication();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"退出应用失败: {ex.Message}");
        }
    }

    public void MinimizeToTray()
    {
        try
        {
            _actionHandler.MinimizeHostWindow();
            ShowNotification("待办便签", "应用已最小化到系统托盘");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"最小化到托盘失败: {ex.Message}");
        }
    }

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

            _trayIcon?.Dispose();
            _trayIcon = null;
            GC.SuppressFinalize(this);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"释放资源失败: {ex.Message}");
        }
    }

    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;
    private const int NIF_MESSAGE = 0x00000001;
    private const int NIF_INFO = 0x00000010;
    private const int NIIF_INFO = 0x00000001;
    private const int IDI_APPLICATION = 32512;

    private const int MF_STRING = 0x00000000;
    private const int MF_SEPARATOR = 0x00000800;
    private const int MF_POPUP = 0x00000010;
    private const int TPM_LEFTALIGN = 0x00000000;
    private const int TPM_TOPALIGN = 0x00000000;
    private const int TPM_RETURNCMD = 0x00000100;

    private const int MenuToggleWidgetMode = 3;
    private const int MenuToggleImmersionMode = 4;
    private const int MenuExit = 5;
    private const int MenuSettings = 6;
    private const int MenuToggleWidgetVisibility = 7;
    private const int MenuAbout = 8;
    private const int MenuImportJson = 9;
    private const int MenuExportJson = 10;
    private const int MenuRestoreBackup = 11;
    private const int MenuHelp = 12;

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NotifyIconData pnid);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr LoadIcon(IntPtr hInstance, int lpIconName);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIDNewItem, string lpNewItem);

    [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "AppendMenu")]
    private static extern bool AppendMenu(IntPtr hMenu, int uFlags, IntPtr uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenu(IntPtr hMenu, int uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr lprc);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

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
