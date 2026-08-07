namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// Строковые идентификаторы игр, доступных в UI. Константы вместо enum.
/// </summary>
public static class GameKinds
{
    public const string Battle = "battle";

    public const string Dialog = "dialog";

    /// <summary>Все доступные игры в порядке отображения в лобби.</summary>
    public static readonly IReadOnlyList<string> All = new[] { Battle, Dialog };
}
