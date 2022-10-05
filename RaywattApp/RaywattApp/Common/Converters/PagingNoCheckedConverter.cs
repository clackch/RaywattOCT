using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class PagingNoCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int nPagingNoIdx = (int)value;
            int nParameter = int.Parse(parameter.ToString());

            if (nPagingNoIdx % 5 == nParameter)
                return "Red";
            else
                return "Black";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
