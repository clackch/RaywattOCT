using RaywattOCTFFR.Common.Bases;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class CodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return "";

            string? classification = parameter.ToString();
            string? code = value.ToString();

            if (string.IsNullOrEmpty(classification) || string.IsNullOrEmpty(code))
                return "";

            if (CodeDefinition.Codes.TryGetValue(classification, out var codeDict) && codeDict.TryGetValue(code, out var label))
                return label;

            return code;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
