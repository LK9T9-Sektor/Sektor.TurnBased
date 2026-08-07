using Sektor.TurnBased.Battle.Model;

namespace Sektor.TurnBased.Battle.Rules;

/// <summary>
/// Условие победы «истребление»: побеждает команда, которой принадлежат все живые акторы.
/// Если живых нет (или живо несколько команд) — бой продолжается (ничья обрабатывается лимитом раундов).
/// </summary>
public sealed class ExterminationCondition : IWinCondition
{
    public string Id { get; }

    public ExterminationCondition(string id) => Id = id;

    public string? WinnerTeamId(BattleState state)
    {
        var aliveTeams = state.AliveActors().Select(a => a.TeamId).Distinct().ToList();
        return aliveTeams.Count == 1 ? aliveTeams[0] : null;
    }
}
