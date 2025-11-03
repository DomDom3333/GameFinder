using System;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ArcadeMatch.Avalonia.Converters;

public class IsLoggedInToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isLoggedIn)
        {
            return new SolidColorBrush(Color.Parse(isLoggedIn ? "#44AA44" : "#FF4444"));
        }

        return AvaloniaProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
