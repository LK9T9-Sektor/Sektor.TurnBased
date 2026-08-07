using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;

namespace Sektor.TurnBased.Battle.Rules;

/// <summary>
/// Стратегия порядка ходов: по состоянию и RNG возвращает список runtime-Id
/// живых акторов в порядке ходов на раунд.
/// </summary>
public interface IOrderRule
{
    string Id { get; }

    IReadOnlyList<string> Order(BattleState state, DeterministicRng rng);
}
