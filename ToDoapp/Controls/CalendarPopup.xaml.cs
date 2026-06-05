using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ToDoapp.Services;

namespace ToDoapp.Controls;

/// <summary>
/// 日历弹出控件 - 带文本框和弹出日历面板的组合控件。
/// 实现按职责拆分为多个 partial 文件：
/// - CalendarPopup.xaml.cs            入口、模板部件、OnApplyTemplate
/// - CalendarPopup.Properties.cs      依赖属性、路由事件、属性变更回调
/// - CalendarPopup.GridInitialization.cs  网格与单元格模板初始化
/// - CalendarPopup.Navigation.cs      按钮与窗口级事件处理
/// - CalendarPopup.Display.cs         显示更新
/// - CalendarPopup.KeyboardFocus.cs   焦点管理与键盘导航
/// - CalendarPopup.Cleanup.cs         主题变更、卸载与弹窗关闭清理
/// </summary>
public partial class CalendarPopup : Control
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

    #region 模板部件名称

    private const string PART_InputBorder = "PART_InputBorder";
    private const string PART_Placeholder = "PART_Placeholder";
    private const string PART_Popup = "PART_Popup";
    private const string PART_PreviousButton = "PART_PreviousButton";
    private const string PART_NextButton = "PART_NextButton";
    private const string PART_YearHeaderButton = "PART_YearHeaderButton";
    private const string PART_MonthHeaderButton = "PART_MonthHeaderButton";
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
    private Button? _yearHeaderButton;
    private Button? _monthHeaderButton;
    private Button? _clearButton;
    private Grid? _monthGrid;
    private Grid? _yearGrid;
    private Grid? _decadeGrid;
    private Window? _parentWindow;

    // 按视图维护的按钮数组，避免依赖 Children 索引
    private Button[,]? _dayButtons;
    private Button[,]? _monthButtons;
    private Button[,]? _yearButtons;

    #endregion

    #region 星期/月份标题

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
        if (_yearHeaderButton != null)
            _yearHeaderButton.Click -= YearHeaderButton_OnClick;
        if (_monthHeaderButton != null)
            _monthHeaderButton.Click -= MonthHeaderButton_OnClick;
        if (_clearButton != null)
            _clearButton.Click -= ClearButton_OnClick;
        if (_popup != null)
            _popup.Closed -= Popup_OnClosed;

        // 获取模板部件
        _inputBorder = GetTemplateChild(PART_InputBorder) as Border;
        _placeholder = GetTemplateChild(PART_Placeholder) as TextBlock;
        _popup = GetTemplateChild(PART_Popup) as Popup;
        _previousButton = GetTemplateChild(PART_PreviousButton) as Button;
        _nextButton = GetTemplateChild(PART_NextButton) as Button;
        _yearHeaderButton = GetTemplateChild(PART_YearHeaderButton) as Button;
        _monthHeaderButton = GetTemplateChild(PART_MonthHeaderButton) as Button;
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
        if (_yearHeaderButton != null)
            _yearHeaderButton.Click += YearHeaderButton_OnClick;
        if (_monthHeaderButton != null)
            _monthHeaderButton.Click += MonthHeaderButton_OnClick;
        if (_clearButton != null)
            _clearButton.Click += ClearButton_OnClick;
        if (_popup != null)
            _popup.Closed += Popup_OnClosed;

        // 订阅主题变更事件
        ThemeService.Instance.ThemeChanged += OnThemeChanged;
        Unloaded += OnUnloaded;

        // 初始化月份视图网格
        InitializeMonthGrid();
        InitializeYearGrid();
        InitializeDecadeGrid();

        UpdateDisplay();
    }
}
