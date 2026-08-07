namespace Sektor.TurnBased.Battle.Model;

/// <summary>
/// Экземпляр статуса на акторе: текущая длительность, источник и снимок модификаторов.
/// Определение (модификаторы, тик-эффект, блок хода) — в StatusDefinition;
/// модификаторы снимаются в момент применения, чтобы состояние не зависело от реестра.
/// </summary>
public sealed class BattleStatus
{
    public string StatusId { get; }
    public int Duration { get; private set; }
    public string SourceActorId { get; }
    public IReadOnlyDictionary<string, int> StatModifiers { get; }
    public bool BlocksTurn { get; }
    public string? TickEffectId { get; }

    public BattleStatus(
        string statusId,
        int duration,
        string sourceActorId,
        IReadOnlyDictionary<string, int> statModifiers,
        bool blocksTurn,
        string? tickEffectId)
    {
        StatusId = statusId;
        Duration = duration;
        SourceActorId = sourceActorId;
        StatModifiers = statModifiers;
        BlocksTurn = blocksTurn;
        TickEffectId = tickEffectId;
    }

    /// <summary>Уменьшает длительность на один ход источника.</summary>
    public void Tick() => Duration--;

    public bool IsExpired => Duration <= 0;
}
