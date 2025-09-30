using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class LongitudeScaleLabelVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            int frameCnt = int.Parse(parameter.ToString());
            int pullbackLength = int.Parse(value.ToString());
            bool lastNumberVisible = (pullbackLength - frameCnt != 1);

            if (pullbackLength >= frameCnt && lastNumberVisible)
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
