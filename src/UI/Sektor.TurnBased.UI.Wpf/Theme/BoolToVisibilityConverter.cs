using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Sektor.TurnBased.UI.Wpf.Theme;

/// <summary>Конвертер: bool в Visibility. Invert — обратное значение (false видимый).</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var flag = value is true;
        if (Invert)
            flag = !flag;
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
