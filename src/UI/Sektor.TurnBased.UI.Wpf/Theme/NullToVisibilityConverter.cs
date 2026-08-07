using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Sektor.TurnBased.UI.Wpf.Theme;

/// <summary>Конвертер: null/non-null в Visibility. Invert — показывать при null.</summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isNull = value is null;
        var show = Invert ? isNull : !isNull;
        return show ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
