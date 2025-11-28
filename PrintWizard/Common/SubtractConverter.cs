using System.Globalization;
using System.Windows.Data;

namespace PrintWizard.Common
{
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
