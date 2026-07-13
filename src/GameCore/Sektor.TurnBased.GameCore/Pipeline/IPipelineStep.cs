using Sektor.TurnBased.GameCore.Battles;

namespace Sektor.TurnBased.GameCore.Pipeline;

/// <summary>
/// Контракт для шага пайплайна.
/// Реализуется в слое GameLogic (Blades, DesperateGods и т.д.).
/// </summary>
public interface IPipelineStep
{
    /// <summary>
    /// Уникальный ID шага (ключ для сериализации и навигации).
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Выполняет логику шага.
    /// Возвращает ID следующего шага.
    /// Если возвращает null — пайплайн приостанавливается (например, ждет ввода от UI).
    /// </summary>
    string? Execute(BattleState state);
}