using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrintWizard.Common
{
    // 转换器：将 bool 转换为 FontWeight 
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
