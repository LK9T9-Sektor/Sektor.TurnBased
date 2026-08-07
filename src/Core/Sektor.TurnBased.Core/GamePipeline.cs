using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Core;

/// <summary>
/// Пайплайн фаз с поддержкой вложенных дочерних пайплайнов
/// (бой внутри рейда, рейд внутри недели) и приостановки фаз.
/// Никогда не бросает исключений: все ошибки через Result.
/// </summary>
public sealed class GamePipeline
{
    private readonly GameContext _context;
    private readonly List<IGamePhase> _phases = new();
    private readonly List<GamePipeline> _children = new();

    private string? _currentPhaseId;
    private string? _suspensionReason;
    private bool _isStarted;
    private bool _isFinished = true;

    public GamePipeline(GameContext context) => _context = context;

    /// <summary>ID текущей фазы (null, если пайплайн не запущен или завершён).</summary>
    public string? CurrentPhaseId => _currentPhaseId;

    /// <summary>Причина текущей приостановки (null, если фаза не ждёт).</summary>
    public string? SuspensionReason => _suspensionReason;

    public bool IsStarted => _isStarted;
    public bool IsSuspended => _suspensionReason is not null;
    public bool IsFinished => _isFinished;

    /// <summary>Дочерние пайплайны в порядке создания.</summary>
    public IReadOnlyList<GamePipeline> Children => _children;

    private IGamePhase? CurrentPhase =>
        _currentPhaseId is null ? null : _phases.FirstOrDefault(p => p.Id == _currentPhaseId);

    /// <summary>Регистрирует фазу до запуска. Повторный Id — ошибка.</summary>
    public Result Register(IGamePhase phase)
    {
        if (_isStarted)
            return Result.Failure("Cannot register phases after the pipeline has started.");
        if (phase is null)
            return Result.Failure("Phase cannot be null.");
        if (string.IsNullOrWhiteSpace(phase.Id))
            return Result.Failure("Phase id cannot be empty.");
        if (_phases.Any(p => p.Id == phase.Id))
            return Result.Failure($"Phase '{phase.Id}' is already registered.");

        _phases.Add(phase);
        phase.Bind(this);
        return Result.Success();
    }

    /// <summary>Создаёт дочерний пайплайн, привязанный к текущему.</summary>
    public GamePipeline CreateChildPipeline()
    {
        var child = new GamePipeline(_context);
        _children.Add(child);
        return child;
    }

    /// <summary>Запускает пайплайн с указанной фазы (вызывает OnEnter).</summary>
    public Result Start(string phaseId)
    {
        if (_isStarted)
            return Result.Failure("Pipeline already started.");
        if (!_phases.Any(p => p.Id == phaseId))
            return Result.Failure($"Phase '{phaseId}' is not registered.");

        _currentPhaseId = phaseId;
        _isStarted = true;
        _isFinished = false;

        var enter = CurrentPhase!.OnEnter(_context);
        if (enter.IsFailure)
        {
            _currentPhaseId = null;
            _isStarted = false;
            _isFinished = true;
            return enter;
        }
        return Result.Success();
    }

    /// <summary>Выполняет один шаг: продвигает активного ребёнка или текущую фазу.</summary>
    public Result Advance()
    {
        if (!_isStarted)
            return Result.Failure("Pipeline is not started.");

        var activeChild = _children.FirstOrDefault(c => !c.IsFinished);
        if (activeChild is not null)
        {
            if (activeChild.IsSuspended)
                return Result.Success();

            var childResult = activeChild.Advance();
            if (childResult.IsFailure)
                return childResult;

            if (activeChild.IsFinished)
            {
                // Дочерний пайплайн завершился — родительская фаза продолжает работу.
                return ExecuteCurrentPhase();
            }
            return Result.Success();
        }

        if (_isFinished || IsSuspended)
            return Result.Success();

        return ExecuteCurrentPhase();
    }

    /// <summary>Повторно выполняет текущую фазу (продолжение после события).</summary>
    public Result Resume()
    {
        if (!_isStarted)
            return Result.Failure("Pipeline is not started.");
        if (_isFinished)
            return Result.Success();
        return ExecuteCurrentPhase();
    }

    /// <summary>Прыжок к указанной фазе (с OnExit текущей и OnEnter целевой).</summary>
    public Result JumpTo(string phaseId)
    {
        if (!_isStarted)
            return Result.Failure("Pipeline is not started.");
        if (!_phases.Any(p => p.Id == phaseId))
            return Result.Failure($"Phase '{phaseId}' is not registered.");

        if (CurrentPhase is not null)
        {
            var exit = CurrentPhase.OnExit(_context);
            if (exit.IsFailure)
                return exit;
        }

        _currentPhaseId = phaseId;
        _suspensionReason = null;
        return CurrentPhase!.OnEnter(_context);
    }

    /// <summary>Передаёт команду активному ребёнку или текущей фазе.</summary>
    public Result ProcessCommand(IGameCommand command)
    {
        if (!_isStarted)
            return Result.Failure("Pipeline is not started.");
        if (command is null)
            return Result.Failure("Command cannot be null.");

        var activeChild = _children.FirstOrDefault(c => !c.IsFinished);
        if (activeChild is not null && activeChild.IsSuspended)
            return activeChild.ProcessCommand(command);

        if (CurrentPhase is null)
            return Result.Failure("No current phase to handle the command.");

        var handled = CurrentPhase.OnCommand(_context, command);
        if (handled.IsFailure)
            return Result.Failure(handled.Error!);

        var transition = handled.Value;
        if (transition is null)
            return Result.Success();

        _suspensionReason = null;
        return ApplyTransition(transition);
    }

    /// <summary>Останавливает пайплайн (и все дочерние).</summary>
    public void Stop()
    {
        foreach (var child in _children)
            child.Stop();

        if (CurrentPhase is not null)
            CurrentPhase.OnExit(_context);

        _currentPhaseId = null;
        _suspensionReason = null;
        _isStarted = false;
        _isFinished = true;
    }

    private Result ExecuteCurrentPhase()
    {
        if (CurrentPhase is null)
            return Result.Failure("No current phase.");

        _suspensionReason = null;
        var executed = CurrentPhase.Execute(_context);
        if (executed.IsFailure)
            return Result.Failure(executed.Error!);
        if (executed.Value is null)
            return Result.Failure("Phase returned a null transition.");
        return ApplyTransition(executed.Value);
    }

    private Result ApplyTransition(PhaseTransition transition)
    {
        if (transition.IsFinished)
        {
            if (CurrentPhase is not null)
            {
                var exit = CurrentPhase.OnExit(_context);
                if (exit.IsFailure)
                    return exit;
            }
            _currentPhaseId = null;
            _isFinished = true;
            return Result.Success();
        }

        if (transition.IsResume)
        {
            _suspensionReason = null;
            return Result.Success();
        }

        if (transition.IsSuspended)
        {
            _suspensionReason = transition.SuspendReason;
            return Result.Success();
        }

        if (transition.NextPhaseId is null)
            return Result.Failure("Transition has no next phase id.");

        var next = _phases.FirstOrDefault(p => p.Id == transition.NextPhaseId);
        if (next is null)
            return Result.Failure($"Phase '{transition.NextPhaseId}' is not registered.");

        if (CurrentPhase is not null)
        {
            var exit = CurrentPhase.OnExit(_context);
            if (exit.IsFailure)
                return exit;
        }

        _currentPhaseId = next.Id;
        return next.OnEnter(_context);
    }
}
