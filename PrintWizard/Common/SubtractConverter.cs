using System;
using System.Globalization;
using System.Windows.Data;

namespace PrintWizard.Common
{
    /// <summary>
    /// 值转换器：将数值减去参数值，结果不小于0
    /// </summary>
    public class SubtractConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double baseValue && parameter is string paramString && double.TryParse(paramString, out double subtractValue))
            {
                return Math.Max(0, baseValue - subtractValue);
            }
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
