using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class FfrStepVisibilityConveter : IValueConverter
    {
        public Visibility TrueValue { get; set; } = Visibility.Visible;

        public Visibility FalseValue { get; set; } = Visibility.Collapsed;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value == null || parameter == null)
                return FalseValue;

            string ffrStep = value.ToString();
            string currentStep = parameter.ToString();

            int nFfrStep = int.Parse(ffrStep[7].ToString());
            int nCurrentStep = int.Parse(currentStep[7].ToString());

            if (nFfrStep > nCurrentStep)
                return TrueValue;

            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
