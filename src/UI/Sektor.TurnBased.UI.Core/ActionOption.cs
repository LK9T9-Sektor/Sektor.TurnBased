namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// Вариант действия игрока в бою: идентификатор, отображаемое имя и режим выбора
/// цели (см. Sektor.TurnBased.Battle.Model.BattleTargetModes).
/// </summary>
public sealed record ActionOption(
    string ActionId,
    string Name,
    string TargetMode);
