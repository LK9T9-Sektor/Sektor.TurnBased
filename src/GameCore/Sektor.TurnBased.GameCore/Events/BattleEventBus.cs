namespace Sektor.TurnBased.GameCore.Events;

/// <summary>
/// Шина событий для транзакционной логики. Реализует паттерн Mediator.
/// Позволяет подписываться на фазы Before (модификация/отмена) и After (реакции/логи).
/// </summary>
public sealed class BattleEventBus
{
    private readonly Dictionary<string, List<Action<BattleActionContext>>> _beforeHooks = new();
    private readonly Dictionary<string, List<Action<BattleActionContext>>> _afterHooks = new();

    public void SubscribeBefore(string actionId, Action<BattleActionContext> handler)
    {
        if (!_beforeHooks.TryGetValue(actionId, out List<Action<BattleActionContext>>? list))
            _beforeHooks[actionId] = [];
        _beforeHooks[actionId].Add(handler);
    }

    public void SubscribeAfter(string actionId, Action<BattleActionContext> handler)
    {
        if (!_afterHooks.TryGetValue(actionId, out List<Action<BattleActionContext>>? list))
            _afterHooks[actionId] = [];
        _afterHooks[actionId].Add(handler);
    }

    /// <summary>
    /// Выполняет транзакцию: Before -> Apply -> After.
    /// Возвращает false, если действие было отменено в фазе Before.
    /// </summary>
    public bool Execute(BattleActionContext context, Action<BattleActionContext> applyBaseLogic)
    {
        if (_beforeHooks.TryGetValue(context.ActionId, out List<Action<BattleActionContext>>? beforeList))
            foreach (Action<BattleActionContext> handler in beforeList)
                handler(context);

        if (context.IsCancelled)
            return false;

        applyBaseLogic(context);

        if (_afterHooks.TryGetValue(context.ActionId, out List<Action<BattleActionContext>>? afterList))
            foreach (Action<BattleActionContext> handler in afterList)
                handler(context);

        return true;
    }
}