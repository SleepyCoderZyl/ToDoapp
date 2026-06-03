using System.Windows;
using System.Threading;
using System;
using ToDoapp.Services;
using ToDoapp.Views;
using System.Windows.Threading;

namespace ToDoapp;

public partial class App : System.Windows.Application
{
    private static Mutex? _mutex;
    private static bool _ownsMutex;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "ToDoApp_SingleInstance", out bool isNewInstance);
        _ownsMutex = isNewInstance;
        
        if (!isNewInstance)
        {
            ActivateExistingInstance();
            Shutdown();
            return;
        }
        
        ThemeService.Instance.Initialize();

        base.OnStartup(e);

        StartupService.Instance.SyncWithSettings();
        _ = WarmupHolidayCalendarAsync();
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var settings = SettingsService.Instance.Settings;
        
        bool isAutoStartLaunch = e.Args.Contains("--autostart");
        bool shouldStartInWidgetMode = isAutoStartLaunch && settings.StartInWidgetMode;
        
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        
        if (shouldStartInWidgetMode)
        {
            mainWindow.ShowInTaskbar = false;
            mainWindow.ShowActivated = false;
            mainWindow.Opacity = 0;
            mainWindow.Loaded += OnStartupWidgetModeWindowLoaded;
        }
        
        mainWindow.Show();
        ScheduleStartupReminder(mainWindow, isAutoStartLaunch);
    }

    private void OnStartupWidgetModeWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow mainWindow)
        {
            return;
        }

        mainWindow.Loaded -= OnStartupWidgetModeWindowLoaded;
        mainWindow.Dispatcher.BeginInvoke(new Action(() =>
        {
            mainWindow.EnterWidgetMode();
        }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex)
        {
            _mutex?.ReleaseMutex();
        }

        _mutex?.Dispose();
        base.OnExit(e);
    }
    
    private void ActivateExistingInstance()
    {
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        foreach (var process in System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName))
        {
            if (process.Id != currentProcess.Id)
            {
                try
                {
                    var handle = process.MainWindowHandle;
                    if (handle != IntPtr.Zero)
                    {
                        MainWindowNativeMethods.ShowWindow(handle, MainWindowNativeMethods.SW_RESTORE);
                        MainWindowNativeMethods.SetForegroundWindow(handle);
                    }
                }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
                {
                    // 激活已有实例失败，可能进程已退出或无访问权限
                    System.Diagnostics.Debug.WriteLine($"激活已有实例失败: {ex.Message}");
                }
            }
        }
    }
    
    private static async Task WarmupHolidayCalendarAsync()
    {
        try
        {
            await HolidayCalendarService.Instance.WarmupAsync(DateTime.Today);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"预热节假日日历失败: {ex.Message}");
        }
    }

    private static void ScheduleStartupReminder(MainWindow mainWindow, bool isAutoStartLaunch)
    {
        if (!isAutoStartLaunch)
        {
            return;
        }

        mainWindow.Dispatcher.BeginInvoke(new Action(() =>
        {
            var settings = SettingsService.Instance.Settings;
            if (!settings.ShowStartupReminderOnAutoStart)
            {
                return;
            }

            var snapshot = StartupReminderService.BuildStartupSnapshot([], settings, DateTime.Now);
            if (!snapshot.HasContent)
            {
                return;
            }

            var reminderWindow = new StartupReminderWindow(mainWindow, snapshot);
            reminderWindow.ShowDialog();
        }), DispatcherPriority.ApplicationIdle);
    }
}
