namespace Sektor.TurnBased.GameCore.Battles;

/// <summary>
/// Плоское состояние боя.
/// Теперь управляется гибким пайплайном, а не жесткой машиной состояний.
/// </summary>
public sealed class BattleState
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int TurnNumber { get; set; } = 1;
    public int Seed { get; set; }

    // ✅ Гибкость: Хранит только ID текущего шага пайплайна.
    // GameLogic сам решает, какие шаги существуют (Initiative, Attack, EndTurn и т.д.)
    public string? CurrentStepId { get; set; }

    public List<string> ActorIds { get; set; } = [];
    public List<string> TurnOrder { get; set; } = [];
    public string? ActiveActorId { get; set; }
    public Dictionary<string, List<string>> Zones { get; set; } = [];
    public List<string> CombatLog { get; set; } = [];
}