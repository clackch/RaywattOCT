using RaywattApp.Common.Bases;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class CodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? classification = parameter.ToString();
            string? code = value.ToString();

            if (classification == null || code == null)
                return "";

            if (!CodeDefinition.Codes[classification].ContainsKey(code))
                return code;

            return CodeDefinition.Codes[classification][code];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
