using RaywattOCTFFR.Common.Bases;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class ScaleBarScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Binding.DoNothing;

            double curScale = (double)value;
            double csSize = (double)parameter;
            double scaleSize = (1 / Constants.ImageResolution) * (Constants.OCTImageSize / csSize) * Constants.ZOffsetScale;
            double orgScaleSize = (1 / Constants.ImageResolution) * Constants.ZOffsetScale;

            double res = orgScaleSize - curScale * scaleSize;

            return res * (csSize / Constants.OCTImageSize);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
