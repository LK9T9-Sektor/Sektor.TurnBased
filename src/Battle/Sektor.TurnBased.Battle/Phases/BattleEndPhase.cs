using Sektor.TurnBased.Battle.Events;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Phases;

/// <summary>
/// Фаза завершения боя: фиксирует победителя (или ничью), поднимает BattleEnded
/// и завершает пайплайн.
/// </summary>
public sealed class BattleEndPhase : IGamePhase
{
    private readonly BattleState _state;

    public string Id => BattlePhaseIds.End;

    public BattleEndPhase(BattleState state) => _state = state;

    public Result<PhaseTransition> Execute(GameContext context)
    {
        _state.IsFinished = true;
        var winner = _state.WinnerTeamId;

        context.Events.Raise(
            new BattleEnded(winner),
            applyBase: e =>
            {
                context.Visuals.Enqueue(new VisualEvent
                {
                    EventType = "BattleEnd",
                    SourceRuntimeId = string.Empty,
                    TargetRuntimeId = e.WinnerTeamId ?? string.Empty,
                });
                context.Log.Append(e.WinnerTeamId is null ? "Battle ended: draw" : $"Battle ended: winner is {e.WinnerTeamId}");
            });

        return Result<PhaseTransition>.Success(PhaseTransition.Finish());
    }
}
