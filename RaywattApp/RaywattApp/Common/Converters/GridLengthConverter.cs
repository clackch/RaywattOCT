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
            if (values == null || values[0] == null || values[1] == null || values.Length != 2 || parameter == null)
                return new GridLength();

            double dValue = 0;
            int frameCnt = int.Parse(parameter.ToString());

            if (Constants.PullbackTypeLong.Equals(values[0].ToString()))
            {
                if(Constants.PullbackLongFrameCnt >= frameCnt)
                {
                    dValue = (double)values[1] / (Constants.PullbackLongFrameCnt / 100);
                }                
            }
            else
            {
                if(Constants.PullbackShortFrameCnt >= frameCnt)
                {
                    dValue = (double)values[1] / (Constants.PullbackShortFrameCnt / 100);
                }
            }

            return new GridLength(dValue);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
