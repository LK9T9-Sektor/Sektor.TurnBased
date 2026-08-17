namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// Снимок юнита (актора/героя) для UI: отображаемые имена, статы, статусы и
/// служебные Id. Агрегируется сессией из игрового состояния, не содержит ссылок
/// на движок. VitalStat — ключевой стат (жизнь) для индикатора, если он задан.
/// PlayerName/PlayerColorHex — мультиплеерные атрибуты владельца слота (пусто в одиночной игре).
/// </summary>
public sealed record UnitSnapshot(
    string RuntimeId,
    string DisplayName,
    string TeamId,
    string TeamDisplayName,
    string TemplateId,
    string ControlledBy,
    bool IsAlive,
    IReadOnlyList<StatValueSnapshot> Stats,
    IReadOnlyList<string> StatusIds,
    StatValueSnapshot? VitalStat = null,
    string? PlayerName = null,
    string? PlayerColorHex = null)
{
    /// <summary>Управляется человеком: слот (player_N) или одиночный игрок (player).</summary>
    public bool IsHumanControlled => ControlledBy != "ai";
}
