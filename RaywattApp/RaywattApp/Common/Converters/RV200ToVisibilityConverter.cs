using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using RaywattApp.Common.Util;

namespace RaywattApp.Common.Converters
{
    class RV200ToVisibilityConverter : IValueConverter
    {
        public Visibility TrueValue { get; set; } = Visibility.Visible;

        public Visibility FalseValue { get; set; } = Visibility.Collapsed;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (CommonUtil.IsRV200())
                return TrueValue;
            else
                return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
