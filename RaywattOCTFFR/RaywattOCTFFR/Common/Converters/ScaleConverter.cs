using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class ScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Binding.DoNothing;

            double scale = (double)value;
            double originValue = Double.Parse(parameter.ToString());

            return originValue / scale;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
