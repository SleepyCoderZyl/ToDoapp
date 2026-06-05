using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace ToDoapp.Controls;

/// <summary>
/// CalendarPopup 的显示更新逻辑。
/// </summary>
public partial class CalendarPopup
{
    /// <summary>更新所有显示内容</summary>
    private void UpdateDisplay()
    {
        UpdatePlaceholder();
        UpdateHeader();
        UpdateMonthView();
        UpdateYearView();
        UpdateDecadeView();
        UpdateViewVisibility();
    }

    /// <summary>更新占位文字可见性</summary>
    private void UpdatePlaceholder()
    {
        if (_placeholder == null) return;
        _placeholder.Visibility = SelectedDate.HasValue ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>更新标题文字</summary>
    private void UpdateHeader()
    {
        // 年份按钮：Month/Year 视图显示具体年份，Decade 视图显示年份范围
        if (_yearHeaderButton != null)
        {
            _yearHeaderButton.Content = CurrentMode == CalendarViewMode.Decade
                ? GetDecadeRange()
                : $"{DisplayDate.Year}年";
        }

        // 月份按钮：在 Month / Year 视图下显示具体月份；Decade 视图下隐藏月份，只看十年范围
        if (_monthHeaderButton != null)
        {
            _monthHeaderButton.Content = CurrentMode == CalendarViewMode.Decade
                ? string.Empty
                : $"{DisplayDate.Month}月";
            _monthHeaderButton.Visibility = CurrentMode == CalendarViewMode.Decade
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    /// <summary>获取十年范围文字（如 2025-2036）</summary>
    private string GetDecadeRange()
    {
        int startYear = (DisplayDate.Year / 10) * 10 - 1;
        int endYear = startYear + 11;
        return $"{startYear}-{endYear}";
    }

    /// <summary>更新视图可见性</summary>
    private void UpdateViewVisibility()
    {
        if (_monthGrid != null)
            _monthGrid.Visibility = CurrentMode == CalendarViewMode.Month ? Visibility.Visible : Visibility.Collapsed;
        if (_yearGrid != null)
            _yearGrid.Visibility = CurrentMode == CalendarViewMode.Year ? Visibility.Visible : Visibility.Collapsed;
        if (_decadeGrid != null)
            _decadeGrid.Visibility = CurrentMode == CalendarViewMode.Decade ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>更新月份视图</summary>
    private void UpdateMonthView()
    {
        if (_monthGrid == null || _dayButtons == null) return;

        var today = DateTime.Today;
        var firstDayOfMonth = new DateTime(DisplayDate.Year, DisplayDate.Month, 1);
        int startDayOfWeek = (int)firstDayOfMonth.DayOfWeek;

        // 收集所有需要显示的日期（固定42天）
        var dates = new List<DateTime>();
        for (int i = startDayOfWeek - 1; i >= 0; i--)
            dates.Add(firstDayOfMonth.AddDays(-(i + 1)));
        var current = firstDayOfMonth;
        while (current.Month == DisplayDate.Month)
        {
            dates.Add(current);
            current = current.AddDays(1);
        }
        while (dates.Count < 42)
        {
            dates.Add(current);
            current = current.AddDays(1);
        }

        // 更新日期单元格
        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                var btn = _dayButtons[row, col];
                if (btn == null) continue;

                int dateIndex = row * 7 + col;
                if (dateIndex >= dates.Count) continue;

                var date = dates[dateIndex];
                btn.Content = date.Day.ToString();
                btn.Tag = date;
                btn.Opacity = 1.0;

                bool isCurrentMonth = date.Month == DisplayDate.Month && date.Year == DisplayDate.Year;
                bool isToday = date.Date == today;
                bool isSelected = SelectedDate.HasValue && date.Date == SelectedDate.Value.Date;

                // 可访问性名称
                string automationName = $"{date:yyyy年MM月dd日}";
                if (isSelected) automationName += "，已选中";
                else if (isToday) automationName += "，今天";
                else if (!isCurrentMonth) automationName += "，非当月";
                AutomationProperties.SetName(btn, automationName);

                // 直接设置 Button 属性，模板通过 TemplateBinding 自动反映
                if (isSelected)
                {
                    // 选中日期：主色实心圆 + 白色文字
                    btn.Background = (Brush)FindResource("PrimaryBrush");
                    btn.BorderBrush = Brushes.Transparent;
                    btn.Foreground = (Brush)FindResource("TextOnPrimaryBrush");
                }
                else if (isToday)
                {
                    // 今日日期：透明背景 + 主色细边框 + 主色文字（空心圆效果）
                    btn.Background = Brushes.Transparent;
                    btn.BorderBrush = (Brush)FindResource("PrimaryBrush");
                    btn.Foreground = (Brush)FindResource("PrimaryBrush");
                }
                else if (!isCurrentMonth)
                {
                    // 非当月：次要文字 + 低透明度
                    btn.Background = Brushes.Transparent;
                    btn.BorderBrush = Brushes.Transparent;
                    btn.Foreground = (Brush)FindResource("TextSecondaryBrush");
                    btn.Opacity = 0.5;
                }
                else
                {
                    // 当月普通日期
                    btn.Background = Brushes.Transparent;
                    btn.BorderBrush = Brushes.Transparent;
                    btn.Foreground = (Brush)FindResource("TextPrimaryBrush");
                }
            }
        }
    }

    /// <summary>更新年份视图</summary>
    private void UpdateYearView()
    {
        if (_yearGrid == null || _monthButtons == null) return;

        int selectedMonth = SelectedDate?.Month ?? -1;
        int selectedYear = SelectedDate?.Year ?? -1;

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                int monthIndex = row * 4 + col;
                var btn = _monthButtons[row, col];
                if (btn == null) continue;

                btn.Content = MonthNames[monthIndex];
                btn.Tag = monthIndex;

                bool isSelectedMonth = (DisplayDate.Year == selectedYear && (monthIndex + 1) == selectedMonth);

                // 直接设置 Button 属性
                btn.Background = isSelectedMonth
                    ? (Brush)FindResource("PrimaryBrush")
                    : Brushes.Transparent;
                btn.Foreground = isSelectedMonth
                    ? (Brush)FindResource("TextOnPrimaryBrush")
                    : (Brush)FindResource("TextPrimaryBrush");
            }
        }
    }

    /// <summary>更新十年视图</summary>
    private void UpdateDecadeView()
    {
        if (_decadeGrid == null || _yearButtons == null) return;

        int baseYear = (DisplayDate.Year / 10) * 10 - 1;
        int selectedYear = SelectedDate?.Year ?? -1;

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                int index = row * 4 + col;
                var btn = _yearButtons[row, col];
                if (btn == null) continue;

                int year = baseYear + index;
                btn.Content = year.ToString();
                btn.Tag = index;
                AutomationProperties.SetName(btn, $"{year}年");

                bool isSelectedYear = year == selectedYear;
                bool isOutOfRange = year < 1 || year > 9999;

                // 直接设置 Button 属性
                btn.Background = isSelectedYear
                    ? (Brush)FindResource("PrimaryBrush")
                    : Brushes.Transparent;
                btn.Foreground = isSelectedYear
                    ? (Brush)FindResource("TextOnPrimaryBrush")
                    : isOutOfRange
                        ? (Brush)FindResource("TextMutedBrush")
                        : (Brush)FindResource("TextPrimaryBrush");
                btn.IsEnabled = !isOutOfRange;
            }
        }
    }
}
