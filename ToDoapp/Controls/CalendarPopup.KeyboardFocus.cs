using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ToDoapp.Controls;

/// <summary>
/// CalendarPopup 的焦点管理与键盘导航。
/// </summary>
public partial class CalendarPopup
{
    /// <summary>Popup 打开后将焦点设置到内容区域</summary>
    private void FocusPopupContent()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsDropDownOpen) return;

            switch (CurrentMode)
            {
                case CalendarViewMode.Month:
                    FocusDayButton();
                    break;
                case CalendarViewMode.Year:
                    FocusMonthButton();
                    break;
                case CalendarViewMode.Decade:
                    FocusYearButton();
                    break;
            }
        }, DispatcherPriority.Render);
    }

    /// <summary>恢复焦点到输入框区域</summary>
    private void RestoreFocusToInput()
    {
        if (_inputBorder != null)
        {
            Dispatcher.BeginInvoke(() => Keyboard.Focus(_inputBorder), DispatcherPriority.Render);
        }
    }

    private void FocusDayButton()
    {
        if (_dayButtons == null) return;

        // 优先聚焦选中日期
        if (SelectedDate.HasValue)
        {
            for (int r = 0; r < 6; r++)
                for (int c = 0; c < 7; c++)
                {
                    var btn = _dayButtons[r, c];
                    if (btn?.Tag is DateTime date && date.Date == SelectedDate.Value.Date)
                    {
                        Keyboard.Focus(btn);
                        return;
                    }
                }
        }

        // 其次是今日
        var today = DateTime.Today;
        for (int r = 0; r < 6; r++)
            for (int c = 0; c < 7; c++)
            {
                var btn = _dayButtons[r, c];
                if (btn?.Tag is DateTime date && date.Date == today)
                {
                    Keyboard.Focus(btn);
                    return;
                }
            }

        // 默认聚焦第一格
        Keyboard.Focus(_dayButtons[0, 0]);
    }

    private void FocusMonthButton()
    {
        if (_monthButtons == null) return;
        int selectedMonth = SelectedDate?.Month ?? -1;
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 4; c++)
            {
                var btn = _monthButtons[r, c];
                if (btn?.Tag is int idx && (idx + 1) == selectedMonth)
                {
                    Keyboard.Focus(btn);
                    return;
                }
            }
        Keyboard.Focus(_monthButtons[0, 0]);
    }

    private void FocusYearButton()
    {
        if (_yearButtons == null) return;
        int selectedYear = SelectedDate?.Year ?? -1;
        int baseYear = (DisplayDate.Year / 10) * 10 - 1;
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 4; c++)
            {
                var btn = _yearButtons[r, c];
                int year = baseYear + (r * 4 + c);
                if (year == selectedYear)
                {
                    Keyboard.Focus(btn);
                    return;
                }
            }
        Keyboard.Focus(_yearButtons[0, 0]);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!IsDropDownOpen) return;

        if (e.Key == Key.Escape)
        {
            IsDropDownOpen = false;
            e.Handled = true;
            return;
        }

        bool handled = false;
        switch (CurrentMode)
        {
            case CalendarViewMode.Month:
                handled = HandleGridKeyNavigation(e.Key, 6, 7, _dayButtons);
                break;
            case CalendarViewMode.Year:
                handled = HandleGridKeyNavigation(e.Key, 3, 4, _monthButtons);
                break;
            case CalendarViewMode.Decade:
                handled = HandleGridKeyNavigation(e.Key, 3, 4, _yearButtons);
                break;
        }

        if (handled)
            e.Handled = true;
    }

    /// <summary>在二维按钮网格中处理方向键导航</summary>
    private static bool HandleGridKeyNavigation(Key key, int rows, int cols, Button[,]? buttons)
    {
        if (buttons == null) return false;

        var focused = Keyboard.FocusedElement as Button;
        if (focused == null) return false;

        int currentRow = -1, currentCol = -1;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (buttons[r, c] == focused)
                {
                    currentRow = r;
                    currentCol = c;
                    break;
                }

        if (currentRow < 0 || currentCol < 0) return false;

        int newRow = currentRow;
        int newCol = currentCol;

        switch (key)
        {
            case Key.Left:
                newCol--;
                if (newCol < 0) { newCol = cols - 1; newRow--; }
                break;
            case Key.Right:
                newCol++;
                if (newCol >= cols) { newCol = 0; newRow++; }
                break;
            case Key.Up:
                newRow--;
                break;
            case Key.Down:
                newRow++;
                break;
            default:
                return false;
        }

        if (newRow < 0 || newRow >= rows || newCol < 0 || newCol >= cols)
            return false;

        var target = buttons[newRow, newCol];
        if (target != null && target.IsEnabled)
        {
            Keyboard.Focus(target);
            return true;
        }

        return false;
    }
}
