namespace Sektor.TurnBased.Core;

/// <summary>
/// Контекст события для хуков Before/After.
/// Позволяет отменить событие на этапе Before или
/// передать дополнительные данные между хуком и базовой логикой.
/// </summary>
public sealed class GameEventContext<TEvent>
{
    public TEvent Event { get; }

    /// <summary>true — событие отменено (устанавливается на этапе Before).</summary>
    public bool IsCancelled { get; set; }

    /// <summary>Дополнительные данные для хуков и базовой логики (опционально).</summary>
    public object? Payload { get; set; }

    public GameEventContext(TEvent @event) => Event = @event;
}
