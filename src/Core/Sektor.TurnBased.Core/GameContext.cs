using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Core;

/// <summary>
/// Контекст выполнения пайплайна: связывает состояние и рантайм-сервисы.
/// </summary>
public sealed class GameContext
{
    public IGameState State { get; }
    public DeterministicRng Rng { get; }
    public GameEventBus Events { get; }
    public VisualQueue Visuals { get; }
    public ContentRegistry Content { get; }
    public GameLog Log { get; }

    public GameContext(
        IGameState state,
        DeterministicRng? rng = null,
        GameEventBus? events = null,
        VisualQueue? visuals = null,
        ContentRegistry? content = null,
        GameLog? log = null)
    {
        State = state;
        Rng = rng ?? new DeterministicRng(Environment.TickCount);
        Events = events ?? new GameEventBus();
        Visuals = visuals ?? new VisualQueue();
        Content = content ?? new ContentRegistry();
        Log = log ?? new GameLog();
    }
}
