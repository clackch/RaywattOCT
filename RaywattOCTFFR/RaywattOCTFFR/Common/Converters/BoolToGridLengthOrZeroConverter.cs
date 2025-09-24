using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class BoolToGridLengthOrZeroConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var singleMode = value is bool b && b;

            if (singleMode) return new GridLength(0);

            if (parameter is string s && double.TryParse(s, out var px))
                return new GridLength(px);

            // 파라미터 없으면 기본은 Star
            return new GridLength(1, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
