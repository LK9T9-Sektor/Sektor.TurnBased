namespace Sektor.TurnBased.Battle.Phases;

/// <summary>Id фаз боя. Строковые константы вместо enum.</summary>
public static class BattlePhaseIds
{
    public const string Setup = "battle_setup";
    public const string RoundStart = "round_start";
    public const string ActorTurn = "actor_turn";
    public const string End = "battle_end";
}
