using RaywattApp.Common.Bases;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    internal class GridLengthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values[0] == null || values[1] == null || values.Length != 2) return new GridLength();

            double dValue = 0;

            if (Constants.PullbackTypeLong.Equals(values[0].ToString()))
            {
                dValue = (double)values[1] / (Constants.PullbackLongFrameCnt / 100);
            }
            else
            {
                dValue = (double)values[1] / (Constants.PullbackShortFrameCnt / 100);

                if (parameter != null && (Constants.PullbackTypeLong.Equals(parameter.ToString())))
                    dValue = 0;
            }

            return new GridLength(dValue);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
