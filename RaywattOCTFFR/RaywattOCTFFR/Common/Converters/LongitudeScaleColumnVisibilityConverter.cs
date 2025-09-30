using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class LongitudeScaleColumnVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            int frameCnt = int.Parse(parameter.ToString());
            int pullbackLength = int.Parse(value.ToString());

            if (pullbackLength >= frameCnt)
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
