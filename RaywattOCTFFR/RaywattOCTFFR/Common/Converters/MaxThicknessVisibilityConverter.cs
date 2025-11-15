using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class MaxThicknessVisibilityConverter : IValueConverter
    {
        public Visibility TrueValue { get; set; } = Visibility.Visible;

        public Visibility FalseValue { get; set; } = Visibility.Collapsed;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || (double)value == -1)
                return FalseValue;

            return TrueValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
