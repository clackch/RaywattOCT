using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class GridLengthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values[0] == null || values[1] == null || values.Length != 2 || parameter == null)
                return new GridLength();

            double dValue = 0;
            int frameCnt = int.Parse(parameter.ToString());

            int pullbackLength = int.Parse(values[0].ToString());

            if(pullbackLength >= frameCnt)
            {
                dValue = (double)values[1] / (pullbackLength / 10.0);
            }                

            return new GridLength(dValue);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
