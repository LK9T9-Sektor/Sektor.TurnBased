using Sektor.TurnBased.GameCore.Actors;
using Sektor.TurnBased.GameCore.Runtime;

namespace Sektor.TurnBased.GameCore.Actions;

/// <summary>
/// Контекст выполнения действия.
/// </summary>
public class ActionContext<TActor, TAction>
    where TActor : BaseActorTemplate
    where TAction : BaseActionTemplate
{
    public required RuntimeActor<TActor> Source { get; init; }
    public required RuntimeActor<TActor> Target { get; init; }
    public required RuntimeInstance<TAction> Action { get; init; }

    /// <summary>
    /// Значение эффекта (урон, лечение). Может быть изменено правилами в фазе Before.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Флаг отмены действия (например, блок, иммунитет или промах).
    /// </summary>
    public bool IsCancelled { get; set; }
}