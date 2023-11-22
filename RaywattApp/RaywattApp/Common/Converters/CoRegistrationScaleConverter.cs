using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RaywattApp.Common.Bases;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    internal class CoRegistrationScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double scale && parameter is double baseSize)
            {
                return baseSize * scale;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
