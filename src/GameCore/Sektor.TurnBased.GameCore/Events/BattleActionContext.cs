using Sektor.TurnBased.GameCore.Entities;

namespace Sektor.TurnBased.GameCore.Events;

/// <summary>
/// Контекст выполнения действия. Используется в BattleEventBus для обработки транзакций.
/// Позволяет модифицировать значения или отменять действие на этапах Before/After.
/// </summary>
public class BattleActionContext
{
    public required BattleActor Source { get; set; }
    public BattleActor? Target { get; set; }
    public required string ActionId { get; set; }
    public int Value { get; set; }
    public bool IsCancelled { get; set; }
}