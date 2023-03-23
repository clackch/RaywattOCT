using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class SettingSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var SettingSelected = App.Current.Resources["SettingSelected"];
            var SettingNotSelected = App.Current.Resources["SettingNotSelected"];

            if (value.Equals(parameter))
            {
                return SettingSelected;
            }
            else
            {
                return SettingNotSelected;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
