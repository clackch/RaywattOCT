using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class PagingNoStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int nPagingNoIdx = (int)value;
            int nParameter = int.Parse(parameter.ToString());
            var PagingSelectedNo = App.Current.Resources["PagingSelectedNo"];
            var PagingNotSelectedNo = App.Current.Resources["PagingNotSelectedNo"];

            if (nPagingNoIdx % 5 == nParameter)
                return PagingSelectedNo;
            else
                return PagingNotSelectedNo;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
