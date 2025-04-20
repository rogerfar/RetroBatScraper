using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RetroBat.Scraper.Converters;

public class TestResultColorConverter : IValueConverter
{
    public Object Convert(Object? value, Type targetType, Object? parameter, CultureInfo culture)
    {
        if (value is String result)
        {
            return result.StartsWith("Successfully")
                ? new SolidColorBrush(Colors.Green)
                : new SolidColorBrush(Colors.Red);
        }

        return new SolidColorBrush(Colors.Black);
    }

    public Object ConvertBack(Object? value, Type targetType, Object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
