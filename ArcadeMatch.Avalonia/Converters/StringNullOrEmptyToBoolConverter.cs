using Avalonia;
using Avalonia.Data.Converters;

namespace ArcadeMatch.Avalonia.Converters;

public class StringNullOrEmptyToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        bool invert = parameter is string text && bool.TryParse(text, out bool parsed) && parsed;
        bool hasText = value is string str && !string.IsNullOrWhiteSpace(str);
        bool result = invert ? !hasText : hasText;
        return result;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return AvaloniaProperty.UnsetValue;
    }
}
