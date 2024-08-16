using RaywattApp.Common.Bases;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    internal class LongitudeScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Binding.DoNothing;

            return Constants.DefaultFoV / (double)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
