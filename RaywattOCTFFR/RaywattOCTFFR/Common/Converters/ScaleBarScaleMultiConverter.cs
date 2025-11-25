using RaywattOCTFFR.Common.Bases;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class ScaleBarScaleMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if(values == null || values[0] == null || values[1] == null || values.Length < 2)
                return Binding.DoNothing;

            double curScale = (double)values[0];
            double csSize = (double)values[1];
            double scaleSize = (1 / Constants.ImageResolution) * (Constants.OCTImageSize / csSize) * Constants.ZOffsetScale;
            double orgScaleSize = (1 / Constants.ImageResolution) * Constants.ZOffsetScale;

            double res = orgScaleSize - curScale * scaleSize;

            return res * (csSize / Constants.OCTImageSize);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
