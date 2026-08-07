using Sektor.TurnBased.Battle.Model;

namespace Sektor.TurnBased.Battle.Rules;

/// <summary>Условие победы: возвращает команду-победителя или null, если бой продолжается.</summary>
public interface IWinCondition
{
    string Id { get; }

    string? WinnerTeamId(BattleState state);
}
