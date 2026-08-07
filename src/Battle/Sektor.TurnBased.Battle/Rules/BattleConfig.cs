namespace Sektor.TurnBased.Battle.Rules;

/// <summary>
/// Конфигурация правил боя: стратегии порядка ходов и победы, лимит раундов (ничья)
/// и критические попадания (шанс и множитель урона; 0 — критов нет).
/// </summary>
public sealed record BattleConfig(
    string OrderRuleId,
    string WinConditionId,
    int? MaxRounds = null,
    double CritChance = 0,
    double CritMultiplier = 1.5);
