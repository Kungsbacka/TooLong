using System;
using System.Globalization;
using System.Windows.Data;
using static TooLong.Scanner;

namespace TooLong
{
    public class ScanErrorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ts = TranslationSource.Instance;
            var error = (ScanError)value;
            switch (error)
            {
                case ScanError.None:
                    return "";
                case ScanError.AccessDenied:
                    return ts["ErrorAccessDenied"];
                case ScanError.IllegalPath:
                    return ts["ErrorIllegalPath"];
                case ScanError.PathNotFound:
                    return ts["ErrorPathNotFound"];
                case ScanError.UnknownError:
                    return ts["ErrorUnknown"];
                default:
                    throw new ArgumentException(nameof(value));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
