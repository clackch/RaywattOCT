using RaywattApp.Common.Bases;
using RaywattApp.Common.Localization;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class CodeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return "";

            string? classification = parameter.ToString();
            string? code = value.ToString();

            if (classification == null || code == null)
                return "";

            if (!CodeDefinition.Codes[classification].ContainsKey(code))
                return code;

            DynamicResource _l10n = (DynamicResource)App.Current.Resources["L10N"];
            return _l10n[CodeDefinition.Codes[classification][code]];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
