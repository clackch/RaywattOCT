using RaywattOCTFFR.Common.Util;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattOCTFFR.Common.Converters
{
    public class NotNullToEnableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if(parameter == null)
                {
                    return true;
                }
                else
                {
                    var item = value as DirectoryItem;
                    if (item.Path.Length == 2)
                        return false;

                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
