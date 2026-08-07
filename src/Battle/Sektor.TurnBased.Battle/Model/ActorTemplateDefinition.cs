namespace Sektor.TurnBased.Battle.Model;

/// <summary>
/// Шаблон актора: базовые статы, доступные действия и способ управления.
/// Используется настройкой боя для создания BattleActor.
/// </summary>
public sealed record ActorTemplateDefinition(
    string Id,
    string TeamId,
    string ControlledBy,
    IReadOnlyDictionary<string, int> BaseStats,
    IReadOnlyList<string> ActionIds);
