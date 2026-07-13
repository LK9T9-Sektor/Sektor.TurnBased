using Sektor.TurnBased.GameCore.States;

namespace Sektor.TurnBased.GameCore.Battles;

/// <summary>
/// Базовый контракт шага пайплайна.
/// Простой по умолчанию, но расширяемый для сложных сценариев.
/// </summary>
public interface IBattleStep
{
    /// <summary>Уникальный ID шага (ключ для сериализации и навигации).</summary>
    string Id { get; }

    /// <summary>
    /// Основная логика шага.
    /// Возвращает ID следующего шага, или null для паузы (ожидание ввода).
    /// </summary>
    string? Execute(BattleState state);

    /// <summary>
    /// Опционально: вызывается при входе в шаг.
    /// По умолчанию пусто — не ломает простые реализации.
    /// </summary>
    void OnEnter(BattleState state) { }

    /// <summary>
    /// Опционально: вызывается при выходе из шага.
    /// </summary>
    void OnExit(BattleState state) { }

    /// <summary>
    /// Опционально: обработка ввода от UI/сети.
    /// Вызывается PipelineManager.ProcessInput().
    /// </summary>
    void OnInput(BattleState state, string actionId, string sourceId, string targetId) { }
}