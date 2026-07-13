using Sektor.TurnBased.GameCore.Actions;
using Sektor.TurnBased.GameCore.Actors;

namespace Sektor.TurnBased.GameCore.Events;

/// <summary>
/// Шина событий для реализации транзакционной логики.
/// </summary>
public sealed class GameEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _before = new();
    private readonly Dictionary<Type, List<Delegate>> _after = new();

    public void SubscribeBefore<TActor, TAction>(Action<ActionContext<TActor, TAction>> handler)
        where TActor : BaseActorTemplate where TAction : BaseActionTemplate
    {
        var key = typeof(ActionContext<TActor, TAction>);
        if (!_before.TryGetValue(key, out var list)) _before[key] = new();
        _before[key].Add(handler);
    }

    public void SubscribeAfter<TActor, TAction>(Action<ActionContext<TActor, TAction>> handler)
        where TActor : BaseActorTemplate where TAction : BaseActionTemplate
    {
        var key = typeof(ActionContext<TActor, TAction>);
        if (!_after.TryGetValue(key, out var list)) _after[key] = new();
        _after[key].Add(handler);
    }

    /// <summary>
    /// Запускает транзакцию: Before -> Apply -> After.
    /// </summary>
    public bool Execute<TActor, TAction>(
        ActionContext<TActor, TAction> context,
        Action<ActionContext<TActor, TAction>> applyBaseLogic)
        where TActor : BaseActorTemplate where TAction : BaseActionTemplate
    {
        var key = typeof(ActionContext<TActor, TAction>);

        // 1. Фаза BEFORE (правила игры: модификация урона, отмена)
        if (_before.TryGetValue(key, out var beforeList))
            foreach (var handler in beforeList) ((Action<ActionContext<TActor, TAction>>)handler)(context);

        if (context.IsCancelled) return false; // Отменено (исключений нет)

        // 2. Фаза APPLY (применение базовой логики)
        applyBaseLogic(context);

        // 3. Фаза AFTER (триггеры, логирование)
        if (_after.TryGetValue(key, out var afterList))
            foreach (var handler in afterList) ((Action<ActionContext<TActor, TAction>>)handler)(context);

        return true;
    }
}