using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ToDoapp.Converters;

/// <summary>
/// 为 Border 生成与其 CornerRadius / BorderThickness 匹配的 PathGeometry Clip，
/// 使子元素被精确裁剪到 Border 的圆角内边界。
/// </summary>
public sealed class BorderClipConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 3)
        {
            return Geometry.Empty;
        }

        if (values[0] is not double width ||
            values[1] is not double height ||
            values[2] is not CornerRadius cornerRadius)
        {
            return Geometry.Empty;
        }

        Thickness borderThickness = values.Length > 3 && values[3] is Thickness thickness
            ? thickness
            : new Thickness(0);

        // 内边界矩形
        double left = borderThickness.Left;
        double top = borderThickness.Top;
        double right = Math.Max(left, width - borderThickness.Right);
        double bottom = Math.Max(top, height - borderThickness.Bottom);

        // 内边界圆角半径：近似 WPF 内部圆角渲染行为
        double topLeft = Math.Max(0, cornerRadius.TopLeft - (borderThickness.Top + borderThickness.Left) * 0.5);
        double topRight = Math.Max(0, cornerRadius.TopRight - (borderThickness.Top + borderThickness.Right) * 0.5);
        double bottomRight = Math.Max(0, cornerRadius.BottomRight - (borderThickness.Bottom + borderThickness.Right) * 0.5);
        double bottomLeft = Math.Max(0, cornerRadius.BottomLeft - (borderThickness.Bottom + borderThickness.Left) * 0.5);

        var figure = new PathFigure
        {
            StartPoint = new Point(left + topLeft, top),
            IsClosed = true
        };

        // 上边
        figure.Segments.Add(new LineSegment(new Point(right - topRight, top), true));
        // 右上角
        if (topRight > 0)
        {
            figure.Segments.Add(new ArcSegment(
                new Point(right, top + topRight),
                new Size(topRight, topRight),
                0, false, SweepDirection.Clockwise, true));
        }
        // 右边
        figure.Segments.Add(new LineSegment(new Point(right, bottom - bottomRight), true));
        // 右下角
        if (bottomRight > 0)
        {
            figure.Segments.Add(new ArcSegment(
                new Point(right - bottomRight, bottom),
                new Size(bottomRight, bottomRight),
                0, false, SweepDirection.Clockwise, true));
        }
        // 下边
        figure.Segments.Add(new LineSegment(new Point(left + bottomLeft, bottom), true));
        // 左下角
        if (bottomLeft > 0)
        {
            figure.Segments.Add(new ArcSegment(
                new Point(left, bottom - bottomLeft),
                new Size(bottomLeft, bottomLeft),
                0, false, SweepDirection.Clockwise, true));
        }
        // 左边
        figure.Segments.Add(new LineSegment(new Point(left, top + topLeft), true));
        // 左上角
        if (topLeft > 0)
        {
            figure.Segments.Add(new ArcSegment(
                new Point(left + topLeft, top),
                new Size(topLeft, topLeft),
                0, false, SweepDirection.Clockwise, true));
        }

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        geometry.Freeze();
        return geometry;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
