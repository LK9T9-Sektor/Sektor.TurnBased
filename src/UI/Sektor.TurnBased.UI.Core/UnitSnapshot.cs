namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// Снимок юнита (актора/героя) для UI: отображаемые имена, статы, статусы и
/// служебные Id. Агрегируется сессией из игрового состояния, не содержит ссылок
/// на движок. VitalStat — ключевой стат (жизнь) для индикатора, если он задан.
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
    StatValueSnapshot? VitalStat = null);
