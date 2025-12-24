using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrintWizard.Common
{
    /// <summary>
    /// 值转换器：将布尔值转换为字体粗细
    /// </summary>
    public class BooleanToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isBold && isBold)
            {
                return FontWeights.Bold;
            }
            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is FontWeight fontWeight && fontWeight == FontWeights.Bold;
        }
    }
}
