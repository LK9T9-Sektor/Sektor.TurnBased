using Sektor.TurnBased.GameCore.Entities;

namespace Sektor.TurnBased.GameCore.States;

/// <summary>
/// Плоское DTO состояния боя. Является единственным источником истины (Single Source of Truth).
/// Сериализуется целиком для Undo/Save/Network. Не содержит бизнес-логики.
/// </summary>
public sealed class BattleState
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");  // ← set вместо init
    public int TurnNumber { get; set; } = 1;
    public int Seed { get; set; }

    /// <summary>ID актёра, который сейчас совершает действие.</summary>
    public string? ActiveActorId { get; set; }

    /// <summary>Очередь ходов. Порядок в списке определяет очередность.</summary>
    public List<string> TurnOrder { get; set; } = [];

    /// <summary>
    /// Зоны поля боя. Ключ: имя зоны ("Hand", "FrontRow", "Grid_0_0").
    /// Значение: список ID актёров в зоне. Позволяет реализовать любые расстановки без хардкода.
    /// </summary>
    public Dictionary<string, List<string>> Zones { get; set; } = new();

    /// <summary>Список всех живых и мёртвых участников боя.</summary>
    public List<BattleActor> Actors { get; set; } = [];

    /// <summary>Текстовый лог для UI и отладки.</summary>
    public List<string> CombatLog { get; set; } = [];
}