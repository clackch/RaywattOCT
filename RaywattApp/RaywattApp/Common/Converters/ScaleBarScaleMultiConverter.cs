using RaywattApp.Common.Bases;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    internal class ScaleBarScaleMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if(values == null || values[0] == null || values[1] == null || values.Length < 2)
                return Binding.DoNothing;

            if (Constants.ImageResolution == 0.0f) return Binding.DoNothing;

            double curScale = (double)values[0];
            double csSize = (double)values[1];
            double scaleSize = (1 / Constants.ImageResolution) * (Constants.OCTImageSize / csSize);
            double orgScaleSize = (1 / Constants.ImageResolution);

            double res = orgScaleSize - curScale * scaleSize;

            return res * (csSize / Constants.OCTImageSize);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
