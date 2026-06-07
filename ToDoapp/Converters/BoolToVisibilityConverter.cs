using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ToDoapp.Converters;

/// <summary>
/// bool → Visibility 转换器。
/// <para>默认：true → Visible，false → Collapsed。</para>
/// <para>传 <c>parameter="invert"</c> 反转。</para>
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isTrue = value is bool b && b;
        if (parameter is string s && string.Equals(s, "invert", StringComparison.OrdinalIgnoreCase))
        {
            isTrue = !isTrue;
        }
        return isTrue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
