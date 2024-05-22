using RaywattApp.Common.Bases;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    internal class LongitudeScaleColumnVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            int frameCnt = int.Parse(parameter.ToString());

            if (int.Parse(value.ToString()) >= frameCnt)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
