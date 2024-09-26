using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace RaywattApp.Common.Converters
{
    public class BoolToVisibilityMultiConverter : IMultiValueConverter
    {
        public Visibility TrueValue { get; set; } = Visibility.Visible;
        public Visibility FalseValue { get; set; } = Visibility.Collapsed;
        public Visibility OCTValue { get; set; } = Visibility.Collapsed;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is bool firstBool && values[1] is bool secondBool)
            {
                if (!firstBool) return OCTValue;
                if(secondBool) return TrueValue;
                else return FalseValue;
            }
            return Binding.DoNothing;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
