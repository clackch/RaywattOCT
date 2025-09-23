using RaywattOCTFFR.Common.Bases;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class UnitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Binding.DoNothing;

            double originValue = (double)value;
            string scale = parameter.ToString();
            double realValue = 0;

            if(scale.Contains("Length"))
            {
                realValue = originValue * Constants.ImageResolution;
            }
            else
            {
                realValue = originValue * Constants.ImageResolution * Constants.ImageResolution;
            }

            return Math.Round(realValue, 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
