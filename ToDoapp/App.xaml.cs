using System.Windows;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;
using System;
using ToDoapp.Services;
using ToDoapp.Views;

namespace ToDoapp;

public partial class App : System.Windows.Application
{
    private static Mutex? _mutex;
    private static bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        StartupDiagnostics.Mark("OnStartup entered");
        _mutex = new Mutex(true, "ToDoApp_SingleInstance", out bool isNewInstance);
        _ownsMutex = isNewInstance;

        if (!isNewInstance)
        {
            ActivateExistingInstance();
            Shutdown();
            return;
        }

        ThemeService.Instance.Initialize();
        StartupDiagnostics.Mark("Theme initialized");

        base.OnStartup(e);

        // 构造并显示主窗口
        var settings = SettingsService.Instance.Settings;
        bool isAutoStartLaunch = e.Args.Contains("--autostart");
        bool shouldStartInWidgetMode = isAutoStartLaunch && settings.StartInWidgetMode;

        var mainWindow = new MainWindow();
        StartupDiagnostics.Mark("MainWindow constructed");
        MainWindow = mainWindow;

        if (shouldStartInWidgetMode)
        {
            mainWindow.ShowInTaskbar = false;
            mainWindow.ShowActivated = false;
        }
        else
        {
            mainWindow.ShowActivated = false;
        }
        EventHandler? postRenderHandler = null;
        postRenderHandler = async (_, _) =>
        {
            mainWindow.ContentRendered -= postRenderHandler;
            try
            {
                await Task.Yield();
                await Task.Run(StartupService.Instance.SyncWithSettings);
                StartupDiagnostics.Mark("Startup registry synchronized");
                await WarmupHolidayCalendarAsync();
                StartupDiagnostics.Mark("Background initialization completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"后台启动任务失败: {ex.Message}");
            }
        };
        mainWindow.ContentRendered += postRenderHandler;
        mainWindow.Show();
        StartupDiagnostics.Mark("MainWindow.Show returned");

        // 正常模式：首帧完成后激活窗口
        if (!shouldStartInWidgetMode)
        {
            mainWindow.Dispatcher.BeginInvoke(new Action(() => mainWindow.Activate()),
                DispatcherPriority.ApplicationIdle);
        }

        ScheduleStartupReminder(mainWindow, isAutoStartLaunch);
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
            await Task.Run(() => HolidayCalendarService.Instance.WarmupAsync(DateTime.Today));
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
