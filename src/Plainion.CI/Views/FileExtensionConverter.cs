using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace Plainion.CI.Views
{
    class FileExtensionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var file = (string)value;
            return "*" + Path.GetExtension(file);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
