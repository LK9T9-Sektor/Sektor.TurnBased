using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;

namespace Sektor.TurnBased.Battle.Rules;

/// <summary>Порядок ходов: порядок создания акторов (фиксированный).</summary>
public sealed class FixedOrderRule : IOrderRule
{
    public string Id { get; }

    public FixedOrderRule(string id) => Id = id;

    public IReadOnlyList<string> Order(BattleState state, DeterministicRng rng) =>
        state.AliveActors().Select(a => a.RuntimeId).ToList();
}
