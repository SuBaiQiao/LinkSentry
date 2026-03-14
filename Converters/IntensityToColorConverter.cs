using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace LinkSentry.Converters;

public class IntensityToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intensity)
        {
            return intensity switch
            {
                0 => Brush.Parse("#20808080"), // Base empty color (visible in both light and dark themes)
                1 => Brush.Parse("#4000C853"), // Light green
                2 => Brush.Parse("#8000C853"), // Medium green
                3 => Brush.Parse("#FF00C853"), // Solid green
                4 => Brush.Parse("#FFFFD600"), // Yellow
                5 => Brush.Parse("#FFFF3D00"), // Red
                _ => Brush.Parse("#20808080")
            };
        }
        return Brush.Parse("#20808080");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
