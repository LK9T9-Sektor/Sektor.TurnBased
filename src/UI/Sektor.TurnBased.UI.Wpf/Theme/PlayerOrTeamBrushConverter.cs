using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Sektor.TurnBased.UI.Core;

namespace Sektor.TurnBased.UI.Wpf.Theme;

/// <summary>
/// Цвет рамки карточки юнита: цвет слота игрока (PlayerColorHex), если задан
/// (мультиплеер), иначе — цвет команды (player/enemy).
/// </summary>
public sealed class PlayerOrTeamBrushConverter : IValueConverter
{
    public Brush? Player { get; set; }

    public Brush? Enemy { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UnitSnapshot unit)
        {
            if (!string.IsNullOrEmpty(unit.PlayerColorHex)
                && StringToBrushConverter.TryParseHex(unit.PlayerColorHex, out var color))
            {
                return new SolidColorBrush(color);
            }

            return unit.TeamId == "player"
                ? Player ?? Enemy ?? Brushes.Gray
                : Enemy ?? Brushes.Gray;
        }

        return Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}