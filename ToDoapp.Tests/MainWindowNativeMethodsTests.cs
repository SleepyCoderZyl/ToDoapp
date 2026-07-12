using ToDoapp.Services;
using Xunit;

namespace ToDoapp.Tests;

public class MainWindowNativeMethodsTests
{
    [Fact]
    public void NormalizeMainWindowStyle_CreatesStandardMinimizableWindowWithoutMaximize()
    {
        var sourceStyle = MainWindowNativeMethods.WS_POPUP |
                          MainWindowNativeMethods.WS_MAXIMIZEBOX;

        var result = MainWindowNativeMethods.NormalizeMainWindowStyle(sourceStyle);

        Assert.Equal(0, result & MainWindowNativeMethods.WS_POPUP);
        Assert.Equal(0, result & MainWindowNativeMethods.WS_MAXIMIZEBOX);
        Assert.NotEqual(0, result & MainWindowNativeMethods.WS_CAPTION);
        Assert.Equal(0, result & MainWindowNativeMethods.WS_SYSMENU);
        Assert.Equal(0, result & MainWindowNativeMethods.WS_MINIMIZEBOX);
        Assert.NotEqual(0, result & MainWindowNativeMethods.WS_THICKFRAME);
    }

    [Fact]
    public void NormalizeDialogWindowStyle_CreatesFixedDialogWithoutTaskbarCommands()
    {
        var sourceStyle = MainWindowNativeMethods.WS_POPUP |
                          MainWindowNativeMethods.WS_MINIMIZEBOX |
                          MainWindowNativeMethods.WS_MAXIMIZEBOX |
                          MainWindowNativeMethods.WS_THICKFRAME;

        var result = MainWindowNativeMethods.NormalizeDialogWindowStyle(sourceStyle);

        Assert.Equal(0, result & MainWindowNativeMethods.WS_POPUP);
        Assert.Equal(0, result & MainWindowNativeMethods.WS_MINIMIZEBOX);
        Assert.Equal(0, result & MainWindowNativeMethods.WS_MAXIMIZEBOX);
        Assert.Equal(0, result & MainWindowNativeMethods.WS_THICKFRAME);
        Assert.Equal(0, result & MainWindowNativeMethods.WS_CAPTION);
        Assert.NotEqual(0, result & MainWindowNativeMethods.WS_SYSMENU);
    }
}
