using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class BoolToEnableConverter : IValueConverter
    {
        public bool TrueValue { get; set; } = true;

        public bool FalseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            
            if (value is bool boolValue)
            {
                if (boolValue)
                    return TrueValue;
                else
                    return FalseValue;
            }

            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (boolValue)
                    return TrueValue;
                else
                    return FalseValue;
            }

            return FalseValue;
        }
    }
}
