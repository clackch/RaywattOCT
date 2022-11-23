using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class SettingSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ButtonImportant = App.Current.Resources["ButtonImportant"];
            var ButtonBasic = App.Current.Resources["ButtonBasic"];

            if (value.Equals(parameter))
            {
                return ButtonImportant;
            }
            else
            {
                return ButtonBasic;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
