using RaywattOCTFFR.Common.Bases;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class CrossSectionScaleMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if(values == null || values[0] == null || values[1] == null || values.Length < 2)
                return Binding.DoNothing;

            double screenSize = Constants.CrossSectionSize;
            double temp = (double)values[1];
            if(temp != 0)
                screenSize = temp;

            double originValue = (double)values[0];
            double scale = Constants.OCTImageSize / screenSize;

            return (originValue * scale);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
