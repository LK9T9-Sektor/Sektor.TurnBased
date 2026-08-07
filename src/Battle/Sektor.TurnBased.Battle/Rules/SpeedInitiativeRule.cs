using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;

namespace Sektor.TurnBased.Battle.Rules;

/// <summary>
/// Порядок ходов: по эффективной инициативе (убывание). При равенстве — детерминированный
/// тас (Fisher–Yates через RNG): одинаковый seed даёт одинаковый порядок.
/// </summary>
public sealed class SpeedInitiativeRule : IOrderRule
{
    public string Id { get; }
    public string InitiativeStatId { get; }

    public SpeedInitiativeRule(string id, string initiativeStatId = "initiative")
    {
        Id = id;
        InitiativeStatId = initiativeStatId;
    }

    public IReadOnlyList<string> Order(BattleState state, DeterministicRng rng)
    {
        var result = new List<string>();
        foreach (var group in state.AliveActors()
                     .GroupBy(a => state.EffectiveStat(a.RuntimeId, InitiativeStatId))
                     .OrderByDescending(g => g.Key))
        {
            var list = group.ToList();
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            result.AddRange(list.Select(a => a.RuntimeId));
        }
        return result;
    }
}
