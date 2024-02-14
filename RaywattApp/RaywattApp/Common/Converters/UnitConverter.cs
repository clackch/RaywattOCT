using RaywattApp.Common.Bases;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    class UnitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Binding.DoNothing;

            double originValue = (double)value;
            string scale = parameter.ToString();
            double realValue = 0;

            double resolution = (scale.Contains("Compare")) ? Constants.ImageResolutionCompare : Constants.ImageResolution;

            if (resolution == 0.0f) return Binding.DoNothing;

            if(scale.Contains("Length"))
            {
                realValue = originValue * resolution;
            }
            else
            {
                realValue = originValue * resolution * resolution;
            }

            return Math.Round(realValue, 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
