using Sektor.TurnBased.GameCore.Extensions;
using Sektor.TurnBased.GameCore.States;

namespace Sektor.TurnBased.GameCore.Pipeline;

/// <summary>
/// Исполнитель цепочки шагов боя. Реализует Chain of Responsibility.
/// Не содержит игровой логики, только управляет переходами между шагами.
/// </summary>
public sealed class BattlePipeline(BattleState state)
{
    private readonly Dictionary<string, IBattleStep> _steps = [];

    public string? CurrentStepId => state.CurrentStepId;
    public IBattleStep? CurrentStep =>
        CurrentStepId is not null && _steps.TryGetValue(CurrentStepId, out IBattleStep? step) ? step : null;

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

        state.CurrentStepId = initialStepId;
        CurrentStep?.OnEnter(state);
        return Result<bool>.Success(true);
    }

    public Result<bool> Advance()
    {
        IBattleStep? step = CurrentStep;
        if (step is null) return Result<bool>.Failure("No active step.");

        step.OnExit(state);
        string? nextId = step.Execute(state);

        if (nextId is null) return Result<bool>.Success(true);

        if (!_steps.ContainsKey(nextId))
            return Result<bool>.Failure($"Next step '{nextId}' not registered.");

        state.CurrentStepId = nextId;
        CurrentStep?.OnEnter(state);
        return Result<bool>.Success(true);
    }

    public void JumpTo(string stepId)
    {
        if (!_steps.ContainsKey(stepId)) return;
        CurrentStep?.OnExit(state);
        state.CurrentStepId = stepId;
        CurrentStep?.OnEnter(state);
    }

    public void ProcessInput(string actionId, string sourceId, string targetId) =>
        CurrentStep?.OnInput(state, actionId, sourceId, targetId);

    public void Clear()
    {
        _steps.Clear();
        state.CurrentStepId = null;
    }
}