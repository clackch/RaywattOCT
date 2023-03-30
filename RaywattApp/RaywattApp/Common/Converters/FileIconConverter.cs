using RaywattApp.Common.Bases;
using RaywattApp.Common.Util;
using System;
using System.Globalization;
using System.Windows.Data;

namespace RaywattApp.Common.Converters
{
    public class FileIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value == null)
                return null;

            var item = value as DirectoryItem;

            if (item == null)
                return null;

            if (item.Path.Length == 2)
                return Constants.FileIconDrive;

            if (item.Name.EndsWith(Constants.FileExtension))
                return Constants.FileIconFile;

            return Constants.FileIconFolder;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
