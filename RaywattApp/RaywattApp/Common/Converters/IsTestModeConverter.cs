using RaywattApp.Common.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class IsTestModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value == null || parameter == null)
                return false;

            return CommonUtil.IsTestMode((Dictionary<string, bool>)value, parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
