namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// Фиксированный пул цветов для отображения игроков (до 4).
/// </summary>
public static class PlayerColors
{
    /// <summary>Цвета по порядку: красный, зелёный, синий, жёлтый.</summary>
    public static readonly string[] All = ["#FF4444", "#44FF44", "#4444FF", "#FFFF44"];

    /// <summary>Возвращает цвет по индексу (0-based, зациклено).</summary>
    public static string Get(int index) => All[index % All.Length];
}