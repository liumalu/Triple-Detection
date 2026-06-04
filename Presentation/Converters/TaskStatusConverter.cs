using System;
using System.Globalization;
using System.Windows.Data;
using TripleDetection.Domain.Enums;

namespace TripleDetection.Presentation.Converters
{
    public class TaskStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskStatus status)
            {
                switch (status)
                {
                    case TaskStatus.Pending:
                        return "待审核";
                    case TaskStatus.Approved:
                        return "已审核";
                    case TaskStatus.Running:
                        return "执行中";
                    case TaskStatus.Completed:
                        return "已完成";
                    default:
                        return status.ToString();
                }
            }
            return value != null ? value.ToString() : "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskStatus status)
                return status;
            return value;
        }
    }
}