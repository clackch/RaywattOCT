using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class SettingSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value.Equals(parameter))
            {
                return "Black";
            }
            else
            {
                return "DarkGray";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
