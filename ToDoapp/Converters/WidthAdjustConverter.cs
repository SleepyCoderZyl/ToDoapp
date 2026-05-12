using System;
using System.Globalization;
using System.Windows.Data;

namespace ToDoapp.Converters;

public sealed class WidthAdjustConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double actualWidth && actualWidth > 0)
        {
            return Math.Max(50, actualWidth - 71);
        }

        return 150;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
