using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class ExpansionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return "0";

            double value = (double)values[0];
            double overall = (double)values[1];

            if (overall == 0)
                return "0";

            return Math.Round(value / overall * 100, 1).ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
