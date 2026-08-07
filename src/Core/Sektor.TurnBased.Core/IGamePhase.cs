using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Core;

/// <summary>
/// Контракт игровой фазы. Фазы реализуют логику игры
/// (старт, цикл хода, бой, город) и живут в проектах конкретных игр.
/// Ядро лишь управляет переходами между ними.
/// Default-методы позволяют простым фазам реализовывать только Execute.
/// </summary>
public interface IGamePhase
{
    /// <summary>Уникальный ID фазы (ключ навигации и сериализации).</summary>
    string Id { get; }

    /// <summary>
    /// Вызывается пайплайном при регистрации фазы: передаёт фазу ссылку на владеющий пайплайн,
    /// чтобы фаза могла создавать дочерние пайплайны. Default — игнорировать.
    /// </summary>
    void Bind(GamePipeline pipeline) { }

    /// <summary>Вызывается при активации фазы.</summary>
    Result OnEnter(GameContext context) => Result.Success();

    /// <summary>
    /// Основная логика фазы: применяет изменения состояния
    /// и возвращает переход — Next, Suspend или Finish.
    /// </summary>
    Result<PhaseTransition> Execute(GameContext context);

    /// <summary>Вызывается перед деактивацией фазы.</summary>
    Result OnExit(GameContext context) => Result.Success();

    /// <summary>
    /// Обработка входящей команды (игрока/UI/сети).
    /// Возвращает null, если фаза продолжает ждать, или переход для продолжения.
    /// </summary>
    Result<PhaseTransition?> OnCommand(GameContext context, IGameCommand command) =>
        Result<PhaseTransition?>.Success(null);
}
