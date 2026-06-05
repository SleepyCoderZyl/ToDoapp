using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ToDoapp.Controls;

/// <summary>
/// CalendarPopup 的网格与单元格模板初始化。
/// </summary>
public partial class CalendarPopup
{
    #region 网格初始化

    /// <summary>初始化月份视图网格（7列 x 7行，固定6行日期）</summary>
    private void InitializeMonthGrid()
    {
        if (_monthGrid == null) return;
        _monthGrid.Children.Clear();
        _monthGrid.ColumnDefinitions.Clear();
        _monthGrid.RowDefinitions.Clear();
        _dayButtons = new Button[6, 7];

        // 定义7列
        for (int i = 0; i < 7; i++)
            _monthGrid.ColumnDefinitions.Add(new ColumnDefinition());

        // 定义7行（1行星期标题 + 固定6行日期）
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

        // 后6行：日期单元格（固定42个）
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
                _dayButtons[row - 1, col] = btn;
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
        _monthButtons = new Button[3, 4];

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
                AutomationProperties.SetName(btn, MonthNames[monthIndex]);
                _yearGrid.Children.Add(btn);
                _monthButtons[row, col] = btn;
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
        _yearButtons = new Button[3, 4];

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
                _yearButtons[row, col] = btn;
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
}
