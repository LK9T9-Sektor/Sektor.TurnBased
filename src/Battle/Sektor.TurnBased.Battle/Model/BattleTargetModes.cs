namespace Sektor.TurnBased.Battle.Model;

/// <summary>Режимы выбора целей действия. Строковые константы вместо enum.</summary>
public static class BattleTargetModes
{
    public const string Self = "self";
    public const string SingleEnemy = "single_enemy";
    public const string AllEnemies = "all_enemies";

    public static readonly IReadOnlyList<string> All = new[] { Self, SingleEnemy, AllEnemies };
}
