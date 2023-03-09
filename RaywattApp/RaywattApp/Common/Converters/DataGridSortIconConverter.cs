using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class DataGridSortIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || String.IsNullOrEmpty(value.ToString()))
                return null;

            if (value.ToString().Contains("▾"))
            {
                return (Style)App.Current.Resources["DataGridColumnHeaderDesc"];
            }
            else if(value.ToString().Contains("▴"))
            {
                return (Style)App.Current.Resources["DataGridColumnHeaderAsc"];
            }
            else
            {
                return (Style)App.Current.Resources["DataGridColumnHeaderDefault"];
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
