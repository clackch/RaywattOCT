using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class DtoPConverter : IValueConverter
    {

        public string TrueValue { get; set; } = "D";

        public string FalseValue { get; set; } = "P";

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
