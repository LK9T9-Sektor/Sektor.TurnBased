using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Sektor.TurnBased.UI.Wpf.Theme;

/// <summary>Цвет карточки юнита по команде: "player" — Player, иначе — Enemy.</summary>
public sealed class TeamToBrushConverter : IValueConverter
{
    public Brush? Player { get; set; }

    public Brush? Enemy { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string teamId && teamId == "player")
            return Player ?? Enemy ?? Brushes.Gray;
        return Enemy ?? Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
