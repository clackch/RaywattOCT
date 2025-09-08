using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class TransparentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is bool boolValue)
            {
                if (boolValue)
                {
                    return Color.Transparent;
                }
                else
                {
                    return Color.Empty;
                }
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter;
        }
    }
}
