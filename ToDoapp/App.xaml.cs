using System.Windows;
using System.Threading;
using System;
using ToDoapp.Services;

namespace ToDoapp;

public partial class App : System.Windows.Application
{
    private static Mutex? _mutex;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "ToDoApp_SingleInstance", out bool isNewInstance);
        
        if (!isNewInstance)
        {
            ActivateExistingInstance();
            Shutdown();
            return;
        }
        
        base.OnStartup(e);
        
        StartupService.Instance.SyncWithSettings();
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
        _mutex?.ReleaseMutex();
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
                        NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);
                        NativeMethods.SetForegroundWindow(handle);
                    }
                }
                catch
                {
                }
            }
        }
    }
    
    private static class NativeMethods
    {
        public const int SW_RESTORE = 9;
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
