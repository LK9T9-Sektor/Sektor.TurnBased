using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// Базовый адаптер игры для UI: управляет пайплайном ядра, накапливает визуальные
/// события и выдаёт снапшоты для отображения. Конкретные игры задают Kind, StartCore
/// и Snapshot. Никогда не бросает исключений: ошибки через Result.
/// </summary>
public abstract class GameSession
{
    private readonly GameContext _context;
    private readonly GamePipeline _pipeline;
    private readonly IReadOnlyDictionary<string, string>? _displayNames;
    private readonly List<VisualEvent> _visuals = new();
    private string? _error;

    /// <summary>Контекст ядра (состояние, RNG, визуалы, лог, контент).</summary>
    protected GameContext Context => _context;

    /// <summary>Идентификатор игры (см. GameKinds).</summary>
    public abstract string Kind { get; }

    /// <summary>true — игра ждёт команду игрока (текущая фаза или вложенная).</summary>
    public bool NeedsInput => IsAwaitingInput(_pipeline);

    public bool IsFinished => _pipeline.IsFinished;

    public bool IsFailed => _error is not null;

    public string? Error => _error;

    public string? CurrentPhaseId => _pipeline.CurrentPhaseId;

    /// <summary>Текстовый журнал игры (только чтение).</summary>
    public IReadOnlyList<string> Log => _context.Log.Entries;

    /// <summary>Накопленные визуальные события, ещё не переданные UI.</summary>
    public IReadOnlyList<VisualEvent> PendingVisuals => _visuals;

    protected GameSession(
        GameContext context,
        GamePipeline pipeline,
        IReadOnlyDictionary<string, string>? displayNames = null)
    {
        _context = context;
        _pipeline = pipeline;
        _displayNames = displayNames;
    }

    /// <summary>Запускает игру и продвигает до ожидания ввода или завершения.</summary>
    public Result Start()
    {
        var started = StartCore();
        if (started.IsFailure)
            return Fail(started.Error!);
        return ContinueUntilGate();
    }

    /// <summary>Выполняет шаг: продвигает пайплайн до ожидания ввода или завершения.</summary>
    public Result Advance()
    {
        if (_pipeline.IsFinished || NeedsInput)
            return Result.Success();

        var advanced = _pipeline.Advance();
        if (advanced.IsFailure)
            return Fail(advanced.Error!);
        return ContinueUntilGate();
    }

    /// <summary>Передаёт команду игрока и продвигает пайплайн до следующего ввода.</summary>
    public Result Submit(IGameCommand command)
    {
        var processed = _pipeline.ProcessCommand(command);
        if (processed.IsFailure)
            return Fail(processed.Error!);
        return ContinueUntilGate();
    }

    /// <summary>Текущий снапшот состояния игры для отображения.</summary>
    public abstract object Snapshot();

    /// <summary>Забирает и очищает накопленные визуальные события.</summary>
    public IReadOnlyList<VisualEvent> TakeVisuals()
    {
        var result = _visuals.ToList();
        _visuals.Clear();
        return result;
    }

    /// <summary>Отображаемое имя по Id: переопределение либо читаемая форма.</summary>
    protected string DisplayNameFor(string id) =>
        _displayNames is not null && _displayNames.TryGetValue(id, out var name)
            ? name
            : DisplayNames.Humanize(id);

    /// <summary>Запуск пайплайна ядра конкретной игры.</summary>
    protected abstract Result StartCore();

    private Result ContinueUntilGate()
    {
        while (!_pipeline.IsFinished && !IsAwaitingInput(_pipeline))
        {
            var advanced = _pipeline.Advance();
            if (advanced.IsFailure)
                return Fail(advanced.Error!);
        }

        DrainVisuals();
        return Result.Success();
    }

    private void DrainVisuals()
    {
        while (_context.Visuals.TryDequeue(out var evt) && evt is not null)
            _visuals.Add(evt);
    }

    private static bool IsAwaitingInput(GamePipeline pipeline) =>
        pipeline.IsSuspended || pipeline.Children.Any(IsAwaitingInput);

    private Result Fail(string error)
    {
        _error = error;
        return Result.Failure(error);
    }
}
