using System;
using System.Windows;

namespace ToDoapp.Controls;

/// <summary>
/// CalendarPopup 的依赖属性、路由事件与属性变更回调。
/// </summary>
public partial class CalendarPopup
{
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
                // 接管"点击 Popup 外部关闭"的逻辑（替代 StaysOpen=False 的鼠标捕获行为）
                ctrl._parentWindow.PreviewMouseDown += ctrl.OnWindowPreviewMouseDown;
            }

            // 将焦点转移到 Popup 内容区域
            ctrl.FocusPopupContent();
        }
        else
        {
            // 关闭弹窗时取消主窗口事件订阅
            if (ctrl._parentWindow != null)
            {
                ctrl._parentWindow.Deactivated -= ctrl.OnWindowDeactivated;
                ctrl._parentWindow.LocationChanged -= ctrl.OnWindowLayoutChanged;
                ctrl._parentWindow.StateChanged -= ctrl.OnWindowLayoutChanged;
                ctrl._parentWindow.PreviewMouseDown -= ctrl.OnWindowPreviewMouseDown;
                ctrl._parentWindow = null;
            }

            // 恢复焦点到输入框
            ctrl.RestoreFocusToInput();
        }
    }

    private static void OnCurrentModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (CalendarPopup)d;
        ctrl.UpdateDisplay();
    }

    #endregion
}
