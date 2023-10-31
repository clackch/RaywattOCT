using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class ComponentVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values[0] == null || values[1] == null || values[2] == null || values[3] == null || values.Length != 4)
                return Visibility.Visible;

            bool value0 = (bool)values[0];
            bool value1 = (bool)values[1];
            bool value2 = (bool)values[2];
            bool value3 = (bool)values[3];

            bool res = value0 && value1 && value2 && value3;

            return res ? Visibility.Collapsed : Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
