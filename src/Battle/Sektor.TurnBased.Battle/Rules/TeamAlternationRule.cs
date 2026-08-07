using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;

namespace Sektor.TurnBased.Battle.Rules;

/// <summary>
/// Порядок ходов: чередование команд. Акторы каждой команды идут в порядке создания,
/// команды чередуются круг за кругом.
/// </summary>
public sealed class TeamAlternationRule : IOrderRule
{
    public string Id { get; }

    public TeamAlternationRule(string id) => Id = id;

    public IReadOnlyList<string> Order(BattleState state, DeterministicRng rng)
    {
        var alive = state.AliveActors().ToList();

        var byTeam = new Dictionary<string, List<string>>();
        foreach (var actor in alive)
        {
            if (!byTeam.TryGetValue(actor.TeamId, out var list))
            {
                list = new List<string>();
                byTeam[actor.TeamId] = list;
            }
            list.Add(actor.RuntimeId);
        }

        var result = new List<string>();
        var teamIds = byTeam.Keys.ToList();
        var max = byTeam.Values.Max(v => v.Count);
        for (var i = 0; i < max; i++)
        {
            foreach (var teamId in teamIds)
            {
                if (i < byTeam[teamId].Count)
                    result.Add(byTeam[teamId][i]);
            }
        }
        return result;
    }
}
