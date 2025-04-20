using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RetroBat.Scraper.Converters;

public class ActiveBorderConverter : IValueConverter
{
    public Object Convert(Object? value, Type targetType, Object? parameter, CultureInfo culture)
    {
        return value is Boolean booleanValue && booleanValue ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Transparent);
    }

    public Object ConvertBack(Object? value, Type targetType, Object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}