using RaywattOCTFFR.Common.Localization;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class L10NConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DynamicResource _l10n = (DynamicResource)App.Current.Resources["L10N"];
            return _l10n[(string)value];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
