using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Sektor.TurnBased.UI.Wpf.Theme;

/// <summary>
/// Конвертер ключа иконки в Geometry из ресурсов приложения (глиф-иконки, как в
/// Blades). Неизвестный ключ — иконка щита, отсутствие приложения — пустая геометрия.
/// </summary>
public sealed class IconKeyToGeometryConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key && TryResolve(key, out var geometry))
            return geometry;
        if (TryResolve("IconShield", out var fallback))
            return fallback;
        return Geometry.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static bool TryResolve(string key, out Geometry geometry)
    {
        geometry = Geometry.Empty;
        if (Application.Current is not { } app || app.Resources is null)
            return false;
        if (!app.Resources.Contains(key))
            return false;
        if (app.Resources[key] is not Geometry resolved)
            return false;
        geometry = resolved;
        return true;
    }
}
