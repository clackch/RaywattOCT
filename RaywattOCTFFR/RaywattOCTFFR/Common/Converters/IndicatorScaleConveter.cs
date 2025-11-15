using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class IndicatorScaleConveter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values[0] == null) 
                return new Thickness(0, 0, 0, 0);

            double scaleSize = Double.Parse(values[0].ToString());
            double position = Double.Parse(parameter.ToString());

            return new Thickness(scaleSize * position, 0, 0, 0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
