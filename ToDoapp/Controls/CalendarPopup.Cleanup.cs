using System;
using System.Windows;
using ToDoapp.Services;

namespace ToDoapp.Controls;

/// <summary>
/// CalendarPopup 的主题变更、卸载与弹窗关闭清理。
/// </summary>
public partial class CalendarPopup
{
    /// <summary>主题变更时刷新显示</summary>
    private void OnThemeChanged(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            InitializeMonthGrid();
            InitializeYearGrid();
            InitializeDecadeGrid();
            UpdateDisplay();
        });
    }

    /// <summary>控件卸载时清理事件订阅</summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ThemeService.Instance.ThemeChanged -= OnThemeChanged;
        if (_popup != null)
            _popup.Closed -= Popup_OnClosed;
        if (_parentWindow != null)
        {
            _parentWindow.Deactivated -= OnWindowDeactivated;
            _parentWindow.LocationChanged -= OnWindowLayoutChanged;
            _parentWindow.StateChanged -= OnWindowLayoutChanged;
            _parentWindow.PreviewMouseDown -= OnWindowPreviewMouseDown;
            _parentWindow = null;
        }
        Unloaded -= OnUnloaded;
    }
}
