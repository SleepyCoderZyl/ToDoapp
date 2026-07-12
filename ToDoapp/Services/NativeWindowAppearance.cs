using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;

namespace ToDoapp.Services;

internal static class NativeWindowAppearance
{
    private const int DarkModeAttribute = 20;
    private const int CornerPreferenceAttribute = 33;

    internal static void ConfigureDialog(Window window, string backgroundResourceKey)
    {
        window.WindowStyle = WindowStyle.None;
        window.AllowsTransparency = false;
        window.SetResourceReference(Window.BackgroundProperty, backgroundResourceKey);
        WindowChrome.SetWindowChrome(window, new WindowChrome
        {
            CaptionHeight = 0,
            CornerRadius = new CornerRadius(8),
            GlassFrameThickness = new Thickness(0),
            ResizeBorderThickness = new Thickness(0),
            UseAeroCaptionButtons = false
        });

        var helper = new WindowInteropHelper(window);
        if (helper.Handle != IntPtr.Zero)
        {
            ApplyDialogAppearance(helper.Handle);
            return;
        }

        EventHandler? sourceInitializedHandler = null;
        sourceInitializedHandler = (_, _) =>
        {
            window.SourceInitialized -= sourceInitializedHandler;
            ApplyDialogAppearance(new WindowInteropHelper(window).Handle);
        };
        window.SourceInitialized += sourceInitializedHandler;
    }

    private static void ApplyDialogAppearance(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var windowStyle = MainWindowNativeMethods.GetWindowLong(
            handle,
            MainWindowNativeMethods.GWL_STYLE);
        MainWindowNativeMethods.SetWindowLong(
            handle,
            MainWindowNativeMethods.GWL_STYLE,
            MainWindowNativeMethods.NormalizeDialogWindowStyle(windowStyle));
        MainWindowNativeMethods.SetWindowPos(
            handle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            MainWindowNativeMethods.SWP_NOSIZE |
            MainWindowNativeMethods.SWP_NOMOVE |
            MainWindowNativeMethods.SWP_NOACTIVATE |
            MainWindowNativeMethods.SWP_FRAMECHANGED);

        try
        {
            var useDarkMode = ThemeService.Instance.IsDarkTheme ? 1 : 0;
            var cornerPreference = 2;
            MainWindowNativeMethods.DwmSetWindowAttribute(
                handle,
                DarkModeAttribute,
                ref useDarkMode,
                Marshal.SizeOf<int>());
            MainWindowNativeMethods.DwmSetWindowAttribute(
                handle,
                CornerPreferenceAttribute,
                ref cornerPreference,
                Marshal.SizeOf<int>());
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }
}
