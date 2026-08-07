namespace Sektor.TurnBased.Battle.Rules;

/// <summary>
/// Конфигурация правил боя: стратегии порядка ходов и победы, лимит раундов (ничья).
/// </summary>
public sealed record BattleConfig(
    string OrderRuleId,
    string WinConditionId,
    int? MaxRounds = null);
