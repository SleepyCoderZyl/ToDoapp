using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ToDoapp.Controls;

/// <summary>
/// CalendarPopup 的按钮与窗口级事件处理。
/// </summary>
public partial class CalendarPopup
{
    #region 模板部件事件

    private void InputBorder_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        IsDropDownOpen = !IsDropDownOpen;
        e.Handled = true;
    }

    /// <summary>
    /// Popup 关闭事件回调（StaysOpen=True 时仅在显式关闭时触发一次）。
    /// 主要用于确保关闭后焦点回到输入框，避免与 IsDropDownOpen 变更回调重复设置。
    /// </summary>
    private void Popup_OnClosed(object? sender, EventArgs e)
    {
        RestoreFocusToInput();
    }

    private void PreviousButton_OnClick(object sender, RoutedEventArgs e)
    {
        switch (CurrentMode)
        {
            case CalendarViewMode.Month:
                DisplayDate = DisplayDate.AddMonths(-1);
                break;
            case CalendarViewMode.Year:
                DisplayDate = DisplayDate.AddYears(-1);
                break;
            case CalendarViewMode.Decade:
                DisplayDate = DisplayDate.AddYears(-10);
                break;
        }
    }

    private void NextButton_OnClick(object sender, RoutedEventArgs e)
    {
        switch (CurrentMode)
        {
            case CalendarViewMode.Month:
                DisplayDate = DisplayDate.AddMonths(1);
                break;
            case CalendarViewMode.Year:
                DisplayDate = DisplayDate.AddYears(1);
                break;
            case CalendarViewMode.Decade:
                DisplayDate = DisplayDate.AddYears(10);
                break;
        }
    }

    private void YearHeaderButton_OnClick(object sender, RoutedEventArgs e)
    {
        // 年份点击：直接进入 Decade 视图（年份选择器），与月份解耦
        if (CurrentMode != CalendarViewMode.Decade)
        {
            CurrentMode = CalendarViewMode.Decade;
        }
    }

    private void MonthHeaderButton_OnClick(object sender, RoutedEventArgs e)
    {
        // 月份点击：直接进入 Year 视图（月份选择器），与年份解耦
        if (CurrentMode != CalendarViewMode.Year)
        {
            CurrentMode = CalendarViewMode.Year;
        }
    }

    private void ClearButton_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedDate = null;
        IsDropDownOpen = false;
    }

    private void DayCell_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is DateTime date)
        {
            SelectedDate = date;
            IsDropDownOpen = false;
        }
    }

    private void MonthCell_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int monthIndex)
        {
            DisplayDate = new DateTime(DisplayDate.Year, monthIndex + 1, 1);
            CurrentMode = CalendarViewMode.Month;
        }
    }

    private void YearCell_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int yearOffset)
        {
            int baseYear = (DisplayDate.Year / 10) * 10 - 1;
            int selectedYear = Math.Clamp(baseYear + yearOffset, 1, 9999);
            DisplayDate = new DateTime(selectedYear, DisplayDate.Month, 1);
            // 选中年份后直接回到 Month 视图，不再强制选月（与月份解耦）
            CurrentMode = CalendarViewMode.Month;
        }
    }

    #endregion

    #region 窗口级事件

    /// <summary>主窗口失活时关闭弹窗</summary>
    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        IsDropDownOpen = false;
    }

    /// <summary>主窗口位置/大小变化时关闭弹窗</summary>
    private void OnWindowLayoutChanged(object? sender, EventArgs e)
    {
        IsDropDownOpen = false;
    }

    /// <summary>
    /// 主窗口 PreviewMouseDown 接管：检测点击是否发生在 Popup 子树之外，
    /// 若是则关闭弹窗（替代 StaysOpen=False 的自动关闭行为，避免"按下闪关"）。
    /// </summary>
    private void OnWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsDropDownOpen) return;
        if (IsInPopupTree(e.OriginalSource as DependencyObject)) return;
        if (IsInInputTree(e.OriginalSource as DependencyObject)) return;
        IsDropDownOpen = false;
    }

    /// <summary>
    /// 判断 DependencyObject 是否位于输入框区域中，避免预览事件先关闭、输入点击再重新打开。
    /// </summary>
    private bool IsInInputTree(DependencyObject? source)
    {
        if (_inputBorder == null) return false;
        if (source == null) return false;

        var d = source;
        while (d != null)
        {
            if (d == _inputBorder) return true;
            d = VisualTreeHelper.GetParent(d) ?? LogicalTreeHelper.GetParent(d);
        }
        return false;
    }

    /// <summary>
    /// 判断 DependencyObject 是否位于 Popup 的可视化子树中。
    /// 注意：Popup.Child 位于独立的 Adorner/顶层窗口中，与 PlacementTarget 不在同一条 visual tree 链上，
    /// 因此直接用引用比较 _popup.Child 比上溯 visual tree 更可靠。
    /// </summary>
    private bool IsInPopupTree(DependencyObject? source)
    {
        if (_popup?.Child == null) return false;
        if (source == null) return false;

        var child = _popup.Child;
        var d = source;
        while (d != null)
        {
            if (d == child) return true;
            d = VisualTreeHelper.GetParent(d) ?? LogicalTreeHelper.GetParent(d);
        }
        return false;
    }

    #endregion
}
