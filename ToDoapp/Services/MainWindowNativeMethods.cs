using System;
using System.Runtime.InteropServices;

namespace ToDoapp.Services;

internal static class MainWindowNativeMethods
{
    // ShowWindow 命令常量
    internal const int SW_RESTORE = 9;

    // 窗口扩展样式常量
    internal const int GWL_STYLE = -16;
    internal const int GWL_EXSTYLE = -20;
    internal const int WS_POPUP = unchecked((int)0x80000000);
    internal const int WS_CAPTION = 0x00C00000;
    internal const int WS_SYSMENU = 0x00080000;
    internal const int WS_THICKFRAME = 0x00040000;
    internal const int WS_MINIMIZEBOX = 0x00020000;
    internal const int WS_MAXIMIZEBOX = 0x00010000;
    internal const int WS_EX_TOOLWINDOW = 0x00000080;
    internal const int WS_EX_TRANSPARENT = 0x00000020;
    internal const int WS_EX_NOACTIVATE = 0x08000000;

    // SetWindowPos 标志
    internal static readonly IntPtr HWND_BOTTOM = new(1);
    internal const uint SWP_NOSIZE = 0x0001;
    internal const uint SWP_NOMOVE = 0x0002;
    internal const uint SWP_NOACTIVATE = 0x0010;
    internal const uint SWP_FRAMECHANGED = 0x0020;

    internal static int NormalizeMainWindowStyle(int windowStyle)
    {
        return (windowStyle &
                ~WS_POPUP &
                ~WS_SYSMENU &
                ~WS_MINIMIZEBOX &
                ~WS_MAXIMIZEBOX) |
               WS_CAPTION |
               WS_THICKFRAME;
    }

    internal static int NormalizeDialogWindowStyle(int windowStyle)
    {
        return (windowStyle &
                ~WS_POPUP &
                ~WS_CAPTION &
                ~WS_MINIMIZEBOX &
                ~WS_MAXIMIZEBOX &
                ~WS_THICKFRAME) |
               WS_SYSMENU;
    }

    [DllImport("user32.dll")]
    internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    internal static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
}
