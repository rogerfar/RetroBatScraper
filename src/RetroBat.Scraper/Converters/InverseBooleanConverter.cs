using System.Globalization;
using System.Windows.Data;

namespace RetroBatScraper.Converters;

public class InverseBooleanConverter : IValueConverter
{
    public Object? Convert(Object? value, Type targetType, Object? parameter, CultureInfo culture)
    {
        if (value is Boolean boolValue)
        {
            return !boolValue;
        }

        return value;
    }

    public Object? ConvertBack(Object? value, Type targetType, Object? parameter, CultureInfo culture)
    {
        if (value is Boolean boolValue)
        {
            return !boolValue;
        }

        return value;
    }
}
