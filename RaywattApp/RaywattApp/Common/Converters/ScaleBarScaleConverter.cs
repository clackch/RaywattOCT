using RaywattApp.Common.Bases;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    internal class ScaleBarScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Binding.DoNothing;

            double curScale = (double)value;
            double csSize = (double)parameter;
            double scaleSize = (1 / Constants.MilimeterPerPixel) * (Constants.OCTImageSize / csSize);
            double orgScaleSize = (1 / Constants.MilimeterPerPixel);

            double res = orgScaleSize - curScale * scaleSize;

            return res * (csSize / Constants.OCTImageSize);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
