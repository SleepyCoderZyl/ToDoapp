using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ToDoapp.Services;

namespace ToDoapp.Controls;

/// <summary>
/// 日历弹出控件 - 带文本框和弹出日历面板的组合控件
/// </summary>
public class CalendarPopup : Control
{
    static CalendarPopup()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CalendarPopup),
            new FrameworkPropertyMetadata(typeof(CalendarPopup)));
    }

    #region 视图状态枚举

    /// <summary>日历视图状态</summary>
    public enum CalendarViewMode
    {
        /// <summary>月份视图</summary>
        Month,
        /// <summary>年份视图</summary>
        Year,
        /// <summary>十年视图</summary>
        Decade
    }

    #endregion

    #region 依赖属性

    /// <summary>选中日期</summary>
    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?), typeof(CalendarPopup),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateChanged));

    /// <summary>当前显示的月份</summary>
    public static readonly DependencyProperty DisplayDateProperty =
        DependencyProperty.Register(nameof(DisplayDate), typeof(DateTime), typeof(CalendarPopup),
            new FrameworkPropertyMetadata(DateTime.Today, OnDisplayDateChanged));

    /// <summary>弹出面板是否打开</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(CalendarPopup),
            new FrameworkPropertyMetadata(false, OnIsDropDownOpenChanged));

    /// <summary>当前视图模式</summary>
    public static readonly DependencyProperty CurrentModeProperty =
        DependencyProperty.Register(nameof(CurrentMode), typeof(CalendarViewMode), typeof(CalendarPopup),
            new FrameworkPropertyMetadata(CalendarViewMode.Month, OnCurrentModeChanged));

    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public DateTime DisplayDate
    {
        get => (DateTime)GetValue(DisplayDateProperty);
        set => SetValue(DisplayDateProperty, value);
    }

    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    public CalendarViewMode CurrentMode
    {
        get => (CalendarViewMode)GetValue(CurrentModeProperty);
        set => SetValue(CurrentModeProperty, value);
    }

    #endregion

    #region 路由事件

    /// <summary>日期选择变更事件</summary>
    public static readonly RoutedEvent SelectedDateChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(SelectedDateChanged), RoutingStrategy.Bubble,
            typeof(RoutedPropertyChangedEventHandler<DateTime?>), typeof(CalendarPopup));

    public event RoutedPropertyChangedEventHandler<DateTime?> SelectedDateChanged
    {
        add => AddHandler(SelectedDateChangedEvent, value);
        remove => RemoveHandler(SelectedDateChangedEvent, value);
    }

    #endregion

    #region 模板部件名称

    private const string PART_InputBorder = "PART_InputBorder";
    private const string PART_Placeholder = "PART_Placeholder";
    private const string PART_Popup = "PART_Popup";
    private const string PART_PreviousButton = "PART_PreviousButton";
    private const string PART_NextButton = "PART_NextButton";
    private const string PART_HeaderButton = "PART_HeaderButton";
    private const string PART_ClearButton = "PART_ClearButton";
    private const string PART_MonthGrid = "PART_MonthGrid";
    private const string PART_YearGrid = "PART_YearGrid";
    private const string PART_DecadeGrid = "PART_DecadeGrid";

    #endregion

    #region 模板部件

    private Border? _inputBorder;
    private TextBlock? _placeholder;
    private Popup? _popup;
    private Button? _previousButton;
    private Button? _nextButton;
    private Button? _headerButton;
    private Button? _clearButton;
    private Grid? _monthGrid;
    private Grid? _yearGrid;
    private Grid? _decadeGrid;
    private Window? _parentWindow;

    #endregion

    #region 星期标题

    private static readonly string[] DayNames = ["日", "一", "二", "三", "四", "五", "六"];
    private static readonly string[] MonthNames = ["1月", "2月", "3月", "4月", "5月", "6月", "7月", "8月", "9月", "10月", "11月", "12月"];

    #endregion

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // 取消旧事件订阅
        if (_inputBorder != null)
            _inputBorder.MouseLeftButtonDown -= InputBorder_OnMouseLeftButtonDown;
        if (_previousButton != null)
            _previousButton.Click -= PreviousButton_OnClick;
        if (_nextButton != null)
            _nextButton.Click -= NextButton_OnClick;
        if (_headerButton != null)
            _headerButton.Click -= HeaderButton_OnClick;
        if (_clearButton != null)
            _clearButton.Click -= ClearButton_OnClick;

        // 获取模板部件
        _inputBorder = GetTemplateChild(PART_InputBorder) as Border;
        _placeholder = GetTemplateChild(PART_Placeholder) as TextBlock;
        _popup = GetTemplateChild(PART_Popup) as Popup;
        _previousButton = GetTemplateChild(PART_PreviousButton) as Button;
        _nextButton = GetTemplateChild(PART_NextButton) as Button;
        _headerButton = GetTemplateChild(PART_HeaderButton) as Button;
        _clearButton = GetTemplateChild(PART_ClearButton) as Button;
        _monthGrid = GetTemplateChild(PART_MonthGrid) as Grid;
        _yearGrid = GetTemplateChild(PART_YearGrid) as Grid;
        _decadeGrid = GetTemplateChild(PART_DecadeGrid) as Grid;

        // 订阅事件
        if (_inputBorder != null)
            _inputBorder.MouseLeftButtonDown += InputBorder_OnMouseLeftButtonDown;
        if (_previousButton != null)
            _previousButton.Click += PreviousButton_OnClick;
        if (_nextButton != null)
            _nextButton.Click += NextButton_OnClick;
        if (_headerButton != null)
            _headerButton.Click += HeaderButton_OnClick;
        if (_clearButton != null)
            _clearButton.Click += ClearButton_OnClick;

        // 订阅主题变更事件
        ThemeService.Instance.ThemeChanged += OnThemeChanged;
        Unloaded += OnUnloaded;

        // 初始化月份视图网格
        InitializeMonthGrid();
        InitializeYearGrid();
        InitializeDecadeGrid();

        UpdateDisplay();
    }

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

    #region 网格初始化

    /// <summary>初始化月份视图网格（7列，行数动态）</summary>
    private void InitializeMonthGrid()
    {
        if (_monthGrid == null) return;
        _monthGrid.Children.Clear();
        _monthGrid.ColumnDefinitions.Clear();
        _monthGrid.RowDefinitions.Clear();

        // 定义7列
        for (int i = 0; i < 7; i++)
            _monthGrid.ColumnDefinitions.Add(new ColumnDefinition());

        // 定义6行（1行星期标题 + 最多5行日期，第6行动态显示）
        for (int i = 0; i < 7; i++)
            _monthGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // 第一行：星期标题
        for (int col = 0; col < 7; col++)
        {
            var textBlock = new TextBlock
            {
                Text = DayNames[col],
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = (Brush)FindResource("TextMutedBrush"),
                Margin = new Thickness(0, 2, 0, 4)
            };
            Grid.SetRow(textBlock, 0);
            Grid.SetColumn(textBlock, col);
            _monthGrid.Children.Add(textBlock);
        }

        // 后6行：日期单元格（最多42个）
        for (int row = 1; row <= 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                var btn = new Button
                {
                    Width = 32,
                    Height = 32,
                    FontSize = 14,
                    Cursor = Cursors.Hand,
                    Tag = null,
                    Template = CreateDayCellTemplate()
                };
                Grid.SetRow(btn, row);
                Grid.SetColumn(btn, col);
                btn.Click += DayCell_OnClick;
                _monthGrid.Children.Add(btn);
            }
        }
    }

    /// <summary>初始化年份视图网格（4列 x 3行）</summary>
    private void InitializeYearGrid()
    {
        if (_yearGrid == null) return;
        _yearGrid.Children.Clear();
        _yearGrid.ColumnDefinitions.Clear();
        _yearGrid.RowDefinitions.Clear();

        for (int i = 0; i < 4; i++)
            _yearGrid.ColumnDefinitions.Add(new ColumnDefinition());
        for (int i = 0; i < 3; i++)
            _yearGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                int monthIndex = row * 4 + col;
                var btn = new Button
                {
                    MinWidth = 56,
                    Height = 34,
                    FontSize = 14,
                    Cursor = Cursors.Hand,
                    Tag = monthIndex,
                    Template = CreateMonthCellTemplate()
                };
                Grid.SetRow(btn, row);
                Grid.SetColumn(btn, col);
                btn.Click += MonthCell_OnClick;
                _yearGrid.Children.Add(btn);
            }
        }
    }

    /// <summary>初始化十年视图网格（4列 x 3行）</summary>
    private void InitializeDecadeGrid()
    {
        if (_decadeGrid == null) return;
        _decadeGrid.Children.Clear();
        _decadeGrid.ColumnDefinitions.Clear();
        _decadeGrid.RowDefinitions.Clear();

        for (int i = 0; i < 4; i++)
            _decadeGrid.ColumnDefinitions.Add(new ColumnDefinition());
        for (int i = 0; i < 3; i++)
            _decadeGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                int yearOffset = row * 4 + col;
                var btn = new Button
                {
                    MinWidth = 56,
                    Height = 34,
                    FontSize = 14,
                    Cursor = Cursors.Hand,
                    Tag = yearOffset,
                    Template = CreateYearCellTemplate()
                };
                Grid.SetRow(btn, row);
                Grid.SetColumn(btn, col);
                btn.Click += YearCell_OnClick;
                _decadeGrid.Children.Add(btn);
            }
        }
    }

    #endregion

    #region 单元格模板

    /// <summary>创建日期单元格模板（通过 TemplateBinding 绑定 Button 属性）</summary>
    private ControlTemplate CreateDayCellTemplate()
    {
        var template = new ControlTemplate(typeof(Button));

        var factory = new FrameworkElementFactory(typeof(Border));
        factory.Name = "Bd";
        // 正圆形状，降低色块存在感
        factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(16));
        // 通过 TemplateBinding 绑定 Button 的 Background/BorderBrush/Foreground
        factory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        factory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        factory.SetValue(Border.BorderThicknessProperty, new Thickness(1));

        var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

        factory.AppendChild(contentPresenter);
        template.VisualTree = factory;

        // 悬停触发器
        var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, FindResource("HoverBrush"), "Bd"));
        template.Triggers.Add(hoverTrigger);

        return template;
    }

    /// <summary>创建月份单元格模板</summary>
    private ControlTemplate CreateMonthCellTemplate()
    {
        var template = new ControlTemplate(typeof(Button));

        var factory = new FrameworkElementFactory(typeof(Border));
        factory.Name = "Bd";
        factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        factory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));

        var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

        factory.AppendChild(contentPresenter);
        template.VisualTree = factory;

        var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, FindResource("HoverBrush"), "Bd"));
        template.Triggers.Add(hoverTrigger);

        return template;
    }

    /// <summary>创建年份单元格模板</summary>
    private ControlTemplate CreateYearCellTemplate()
    {
        return CreateMonthCellTemplate();
    }

    #endregion

    #region 事件处理

    private void InputBorder_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        IsDropDownOpen = !IsDropDownOpen;
        e.Handled = true;
    }

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

    private void HeaderButton_OnClick(object sender, RoutedEventArgs e)
    {
        switch (CurrentMode)
        {
            case CalendarViewMode.Month:
                CurrentMode = CalendarViewMode.Year;
                break;
            case CalendarViewMode.Year:
                CurrentMode = CalendarViewMode.Decade;
                break;
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
            CurrentMode = CalendarViewMode.Year;
        }
    }

    #endregion

    #region 属性变更回调

    private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (CalendarPopup)d;
        ctrl.UpdateDisplay();
        ctrl.RaiseEvent(new RoutedPropertyChangedEventArgs<DateTime?>((DateTime?)e.OldValue, (DateTime?)e.NewValue, SelectedDateChangedEvent));
    }

    private static void OnDisplayDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (CalendarPopup)d;
        ctrl.UpdateDisplay();
    }

    private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (CalendarPopup)d;
        if ((bool)e.NewValue)
        {
            // 打开弹窗时重置为月份视图
            ctrl.CurrentMode = CalendarViewMode.Month;
            if (ctrl.SelectedDate.HasValue)
                ctrl.DisplayDate = ctrl.SelectedDate.Value;

            // 订阅主窗口事件，确保弹窗不会常驻桌面
            ctrl._parentWindow = Window.GetWindow(ctrl);
            if (ctrl._parentWindow != null)
            {
                ctrl._parentWindow.Deactivated += ctrl.OnWindowDeactivated;
                ctrl._parentWindow.LocationChanged += ctrl.OnWindowLayoutChanged;
                ctrl._parentWindow.StateChanged += ctrl.OnWindowLayoutChanged;
            }
        }
        else
        {
            // 关闭弹窗时取消主窗口事件订阅
            if (ctrl._parentWindow != null)
            {
                ctrl._parentWindow.Deactivated -= ctrl.OnWindowDeactivated;
                ctrl._parentWindow.LocationChanged -= ctrl.OnWindowLayoutChanged;
                ctrl._parentWindow.StateChanged -= ctrl.OnWindowLayoutChanged;
                ctrl._parentWindow = null;
            }
        }
    }

    private static void OnCurrentModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (CalendarPopup)d;
        ctrl.UpdateDisplay();
    }

    #endregion

    #region 显示更新

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
        if (_headerButton == null) return;
        _headerButton.Content = CurrentMode switch
        {
            CalendarViewMode.Month => $"{DisplayDate.Year}年{DisplayDate.Month}月",
            CalendarViewMode.Year => $"{DisplayDate.Year}年",
            CalendarViewMode.Decade => GetDecadeRange(),
            _ => string.Empty
        };
    }

    /// <summary>获取十年范围文字</summary>
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
        if (_monthGrid == null) return;

        var today = DateTime.Today;
        var firstDayOfMonth = new DateTime(DisplayDate.Year, DisplayDate.Month, 1);
        int startDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
        int daysInMonth = DateTime.DaysInMonth(DisplayDate.Year, DisplayDate.Month);

        // 计算需要的行数（5或6）
        int totalCells = startDayOfWeek + daysInMonth;
        int neededRows = (totalCells + 6) / 7;

        // 动态设置第6行高度
        if (_monthGrid.RowDefinitions.Count > 6)
        {
            _monthGrid.RowDefinitions[6].Height = neededRows > 5
                ? GridLength.Auto
                : new GridLength(0);
        }

        // 收集所有需要显示的日期
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
        int childIndex = 7; // 跳过星期标题
        for (int row = 1; row <= 6; row++)
        {
            // 跳过不需要的行
            if (row > neededRows)
            {
                childIndex += 7;
                continue;
            }

            for (int col = 0; col < 7; col++)
            {
                if (childIndex >= _monthGrid.Children.Count) break;
                var btn = _monthGrid.Children[childIndex] as Button;
                if (btn == null) { childIndex++; continue; }

                int dateIndex = (row - 1) * 7 + col;
                if (dateIndex >= dates.Count) { childIndex++; continue; }

                var date = dates[dateIndex];
                btn.Content = date.Day.ToString();
                btn.Tag = date;
                btn.Opacity = 1.0;

                bool isCurrentMonth = date.Month == DisplayDate.Month && date.Year == DisplayDate.Year;
                bool isToday = date.Date == today;
                bool isSelected = SelectedDate.HasValue && date.Date == SelectedDate.Value.Date;

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

                childIndex++;
            }
        }
    }

    /// <summary>更新年份视图</summary>
    private void UpdateYearView()
    {
        if (_yearGrid == null) return;

        int selectedMonth = SelectedDate?.Month ?? -1;
        int selectedYear = SelectedDate?.Year ?? -1;

        for (int i = 0; i < 12; i++)
        {
            var btn = _yearGrid.Children[i] as Button;
            if (btn == null) continue;

            btn.Content = MonthNames[i];
            btn.Tag = i;

            bool isSelectedMonth = (DisplayDate.Year == selectedYear && (i + 1) == selectedMonth);

            // 直接设置 Button 属性
            btn.Background = isSelectedMonth
                ? (Brush)FindResource("PrimaryBrush")
                : Brushes.Transparent;
            btn.Foreground = isSelectedMonth
                ? (Brush)FindResource("TextOnPrimaryBrush")
                : (Brush)FindResource("TextPrimaryBrush");
        }
    }

    /// <summary>更新十年视图</summary>
    private void UpdateDecadeView()
    {
        if (_decadeGrid == null) return;

        int baseYear = (DisplayDate.Year / 10) * 10 - 1;
        int selectedYear = SelectedDate?.Year ?? -1;

        for (int i = 0; i < 12; i++)
        {
            var btn = _decadeGrid.Children[i] as Button;
            if (btn == null) continue;

            int year = baseYear + i;
            btn.Content = year.ToString();
            btn.Tag = i;

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

    #endregion

    #region 键盘支持

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape && IsDropDownOpen)
        {
            IsDropDownOpen = false;
            e.Handled = true;
        }
    }

    #endregion

    #region 清理

    /// <summary>控件卸载时清理事件订阅</summary>
    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ThemeService.Instance.ThemeChanged -= OnThemeChanged;
        if (_parentWindow != null)
        {
            _parentWindow.Deactivated -= OnWindowDeactivated;
            _parentWindow.LocationChanged -= OnWindowLayoutChanged;
            _parentWindow.StateChanged -= OnWindowLayoutChanged;
            _parentWindow = null;
        }
        Unloaded -= OnUnloaded;
    }

    #endregion
}
