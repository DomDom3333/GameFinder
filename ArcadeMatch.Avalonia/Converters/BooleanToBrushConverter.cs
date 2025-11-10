using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ArcadeMatch.Avalonia.Converters;

public class BooleanToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool flag)
        {
            return new SolidColorBrush(flag ? Colors.LimeGreen : Colors.Gray);
        }

        return AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
