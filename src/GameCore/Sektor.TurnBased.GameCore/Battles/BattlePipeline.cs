using Sektor.TurnBased.GameCore.Extensions;

namespace Sektor.TurnBased.GameCore.Battles;

/// <summary>
/// Исполняет цепочку шагов боя.
/// Реализует паттерн Chain of Responsibility.
/// </summary>
public sealed class BattlePipeline
{
    private readonly BattleState _state;
    private readonly Dictionary<string, IBattleStep> _steps = new();

    public string? CurrentStepId => _state.CurrentStepId;
    public IBattleStep? CurrentStep =>
        CurrentStepId is not null && _steps.TryGetValue(CurrentStepId, out var step) ? step : null;

    public BattlePipeline(BattleState state) => _state = state;

    public void Register(IBattleStep step)
    {
        if (string.IsNullOrWhiteSpace(step.Id))
            throw new ArgumentException("Step ID cannot be empty.", nameof(step));
        _steps[step.Id] = step;
    }

    public Result<bool> Start(string initialStepId)
    {
        if (!_steps.ContainsKey(initialStepId))
            return Result<bool>.Failure($"Step '{initialStepId}' not registered.");

        _state.CurrentStepId = initialStepId;
        CurrentStep?.OnEnter(_state);
        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Выполняет текущий шаг и переходит к следующему.
    /// </summary>
    public Result<bool> Advance()
    {
        var step = CurrentStep;
        if (step is null)
            return Result<bool>.Failure("No active step.");

        step.OnExit(_state);
        var nextId = step.Execute(_state);

        if (nextId is null)
            return Result<bool>.Success(true); // Пауза: ждём ввода

        if (!_steps.ContainsKey(nextId))
            return Result<bool>.Failure($"Next step '{nextId}' not registered.");

        _state.CurrentStepId = nextId;
        CurrentStep?.OnEnter(_state);
        return Result<bool>.Success(true);
    }

    public void JumpTo(string stepId)
    {
        if (!_steps.ContainsKey(stepId)) return;
        CurrentStep?.OnExit(_state);
        _state.CurrentStepId = stepId;
        CurrentStep?.OnEnter(_state);
    }

    public void ProcessInput(string actionId, string sourceId, string targetId) =>
        CurrentStep?.OnInput(_state, actionId, sourceId, targetId);

    public void Clear()
    {
        _steps.Clear();
        _state.CurrentStepId = null;
    }
}