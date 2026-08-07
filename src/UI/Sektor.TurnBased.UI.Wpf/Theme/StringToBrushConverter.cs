using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Sektor.TurnBased.UI.Wpf.Theme;

/// <summary>
/// Конвертер HEX-цвета в кисть: "#RRGGBB" или "#AARRGGBB" (палитра Blades).
/// Некорректное значение — серый.
/// </summary>
public sealed class StringToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && TryParse(hex, out var color))
            return new SolidColorBrush(color);

        return Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static bool TryParse(string hex, out Color color)
    {
        color = Colors.Transparent;
        if (string.IsNullOrEmpty(hex) || hex[0] != '#' || hex.Length is not (7 or 9))
            return false;

        var span = hex.AsSpan(1);
        var alpha = byte.MaxValue;
        if (span.Length == 8)
        {
            if (!TryHexByte(span[..2], out alpha))
                return false;
            span = span[2..];
        }

        if (!TryHexByte(span[..2], out var red)
            || !TryHexByte(span.Slice(2, 2), out var green)
            || !TryHexByte(span.Slice(4, 2), out var blue))
        {
            return false;
        }

        color = Color.FromArgb(alpha, red, green, blue);
        return true;
    }

    private static bool TryHexByte(ReadOnlySpan<char> text, out byte value)
    {
        value = 0;
        if (text.Length != 2
            || !byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        value = parsed;
        return true;
    }
}
