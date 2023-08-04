using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class RadioButtonVisibilityConverter : IValueConverter
    {
        public Visibility TrueValue { get; set; } = Visibility.Visible;

        public Visibility FalseValue { get; set; } = Visibility.Collapsed;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return FalseValue;

            if (value.ToString() == parameter.ToString())
                return TrueValue;
            else
                return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter;
        }
    }
}
