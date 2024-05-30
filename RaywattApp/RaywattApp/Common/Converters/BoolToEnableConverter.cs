using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    internal class BoolToEnableConverter : IValueConverter
    {
        public bool TrueValue { get; set; } = true;

        public bool FalseValue { get; set; } = false;

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
            throw new NotImplementedException();
        }
    }
}
