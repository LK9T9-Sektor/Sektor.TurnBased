using Sektor.TurnBased.GameCore.States;

namespace Sektor.TurnBased.GameCore.Pipeline;

/// <summary>
/// Контракт шага пайплайна боя.
/// Default-методы позволяют простым шагам реализовывать только Execute.
/// </summary>
public interface IBattleStep
{
    /// <summary>Уникальный ID шага (ключ для навигации и сериализации).</summary>
    string Id { get; }

    /// <summary>
    /// Основная логика шага.
    /// Возвращает ID следующего шага или null для приостановки (ожидание ввода UI).
    /// </summary>
    string? Execute(BattleState state);

    /// <summary>Вызывается при активации шага.</summary>
    void OnEnter(BattleState state) { }

    /// <summary>Вызывается перед деактивацией шага.</summary>
    void OnExit(BattleState state) { }

    /// <summary>Вызывается при получении ввода от UI/сети.</summary>
    void OnInput(BattleState state, string actionId, string sourceId, string targetId) { }
}