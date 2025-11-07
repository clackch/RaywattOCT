using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class GridMarginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if(values == null || values[0] == null || values[1] == null || values.Length != 2) return new Thickness(0, 0, 0, 0);

            double dValue = (double)values[1] / (int.Parse(values[0].ToString()) / 10);

            if (parameter != null)
            {
                return new Thickness(0, 0, -1 * dValue, 0);
            }
            else
            {
                return new Thickness(-1 * dValue, 0, 0, 0);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
