using System;
using RaywattOCTFFR.Common.Bases;
using System.Globalization;
using System.Windows.Data;
using Point = System.Windows.Point;

namespace RaywattOCTFFR.Common.Converters
{
    public class CoRegistrationTranslateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Binding.DoNothing;

            Point mousePosition = (Point)value;
            double scale = Constants.CoRegZoomScale;
            double imageRatio = Constants.CoRegZoomAngioSize / Constants.AngioSize;

            double translateValue;
            if (parameter.ToString() == "X")
            {
                translateValue = -mousePosition.X * scale * imageRatio + (Constants.CoRegZoomAngioSize/2);
            }
            else
            {
                translateValue = -mousePosition.Y * scale * imageRatio + (Constants.CoRegZoomAngioSize/2);
            }

            return translateValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
