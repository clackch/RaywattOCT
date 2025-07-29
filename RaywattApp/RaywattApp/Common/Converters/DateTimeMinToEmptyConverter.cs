using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class DateTimeMinToEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
        {
            if (dt == DateTime.MinValue)
                return string.Empty;

            return dt.ToString("g", culture);
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(value as string))
            return DateTime.MinValue;

        if (DateTime.TryParse(value as string, culture, DateTimeStyles.None, out var result))
            return result;

        return DateTime.MinValue;
    }
}
}
