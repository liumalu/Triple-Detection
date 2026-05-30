using System;
using System.Globalization;
using System.Windows.Data;

namespace TripleDetection.Converters
{
    public class LoginButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isLoading && isLoading)
                return "登录中...";
            return parameter?.ToString() ?? "登 录";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}