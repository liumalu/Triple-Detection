using System;
using System.Globalization;
using System.Windows.Data;
using TripleDetection.Domain.Enums;

namespace TripleDetection.Presentation.Converters
{
    public class ProductStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ProductStatus status)
            {
                return status switch
                {
                    ProductStatus.Active => "启用",
                    ProductStatus.Inactive => "停用",
                    _ => status.ToString()
                };
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ComboBox SelectedItem 绑定到 ProductStatus 枚举，WPF 会自动处理类型转换
            if (value is ProductStatus status)
                return status;
            return value;
        }
    }
}