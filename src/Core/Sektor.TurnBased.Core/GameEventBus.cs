namespace Sektor.TurnBased.Core;

/// <summary>
/// Шина доменных событий. Перед применением базовой логики выполняются
/// хуки Before (могут отменить событие), после — хуки After.
/// Никогда не бросает исключений: ошибки подписчиков изолируются.
/// </summary>
public sealed class GameEventBus
{
    private sealed class SubscriptionList
    {
        public readonly List<Delegate> Before = new();
        public readonly List<Delegate> After = new();
    }

    private readonly Dictionary<Type, SubscriptionList> _subscriptions = new();

    public void SubscribeBefore<TEvent>(Action<GameEventContext<TEvent>> handler) =>
        GetOrCreate(typeof(TEvent)).Before.Add(handler);

    public void SubscribeAfter<TEvent>(Action<GameEventContext<TEvent>> handler) =>
        GetOrCreate(typeof(TEvent)).After.Add(handler);

    public bool Raise<TEvent>(TEvent @event, Action<TEvent> applyBase)
    {
        if (!_subscriptions.TryGetValue(typeof(TEvent), out var subs))
        {
            applyBase?.Invoke(@event);
            return true;
        }

        var context = new GameEventContext<TEvent>(@event);
        foreach (var handler in subs.Before)
        {
            if (handler is Action<GameEventContext<TEvent>> typed)
            {
                try
                {
                    typed(context);
                }
                catch
                {
                    // Подписчик бросил исключение — изолируем, не рушим игру.
                }
            }
        }

        if (context.IsCancelled)
            return false;

        try
        {
            applyBase?.Invoke(@event);
        }
        catch
        {
            return false;
        }

        foreach (var handler in subs.After)
        {
            if (handler is Action<GameEventContext<TEvent>> typed)
            {
                try
                {
                    typed(context);
                }
                catch
                {
                    // Изолируем ошибку подписчика.
                }
            }
        }

        return true;
    }

    private SubscriptionList GetOrCreate(Type eventType)
    {
        if (!_subscriptions.TryGetValue(eventType, out var subs))
        {
            subs = new SubscriptionList();
            _subscriptions[eventType] = subs;
        }
        return subs;
    }
}
