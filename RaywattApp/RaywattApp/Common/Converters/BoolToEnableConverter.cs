using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    internal class BoolToEnableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is bool boolValue)
            {
                if (boolValue)
                    return true;
                else
                    return false;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
