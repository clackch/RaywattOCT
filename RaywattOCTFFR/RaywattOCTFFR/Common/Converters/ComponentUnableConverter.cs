using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class ComponentUnableConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return true;

            bool allTrue = values.All(v => v is bool b && b);

            return allTrue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
