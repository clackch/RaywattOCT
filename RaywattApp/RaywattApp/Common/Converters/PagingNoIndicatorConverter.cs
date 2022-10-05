using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class PagingNoIndicatorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
