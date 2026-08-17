using Sektor.TurnBased.Battle.Commands;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Battle.Rules;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Phases;

/// <summary>
/// Фаза хода актора: выбирает текущего живого и не заблокированного актора.
/// Человек (ControlledBy "player" или слот "player_N") — приостановка до команды;
/// враг — ход через AI. После каждого хода проверяется условие победы и конец порядка.
/// </summary>
public sealed class ActorTurnPhase : IGamePhase
{
    private readonly BattleState _state;
    private readonly BattleRules _rules;
    private readonly BattleExecutor _executor;
    private readonly BattleAi _ai;
    private readonly BattleEventSink _sink;

    public string Id => BattlePhaseIds.ActorTurn;

    public ActorTurnPhase(
        BattleState state,
        BattleRules rules,
        BattleExecutor executor,
        BattleAi ai,
        BattleEventSink sink)
    {
        _state = state;
        _rules = rules;
        _executor = executor;
        _ai = ai;
        _sink = sink;
    }

    public Result<PhaseTransition> Execute(GameContext context)
    {
        if (_state.WinnerTeamId is not null)
            return Result<PhaseTransition>.Success(PhaseTransition.Next(BattlePhaseIds.End));

        while (true)
        {
            var currentId = CurrentActorId();
            if (currentId is null)
            {
                var winner = _rules.WinCondition.WinnerTeamId(_state);
                if (winner is not null || IsMaxRoundsReached())
                {
                    _state.WinnerTeamId = winner;
                    return Result<PhaseTransition>.Success(PhaseTransition.Next(BattlePhaseIds.End));
                }
                return Result<PhaseTransition>.Success(PhaseTransition.Next(BattlePhaseIds.RoundStart));
            }

            var actor = _state.GetActor(currentId)!;
            context.Visuals.Enqueue(new VisualEvent
            {
                EventType = "TurnStart",
                SourceRuntimeId = currentId,
                TargetRuntimeId = currentId,
            });
            context.Log.Append($"Turn: {currentId}");

            if (HasBlockingStatus(actor))
            {
                _sink.TurnBlocked(currentId);
                _state.TurnIndex++;
                continue;
            }

            if (actor.IsHumanControlled)
                return Result<PhaseTransition>.Success(PhaseTransition.Suspend("awaiting_command"));

            var command = _ai.ChooseCommand(currentId);
            if (command is null)
            {
                _state.TurnIndex++;
                continue;
            }

            var result = _executor.Execute(command);
            if (result.IsFailure)
                return Result<PhaseTransition>.Failure(result.Error!);

            _state.TurnIndex++;

            var afterActionWinner = _rules.WinCondition.WinnerTeamId(_state);
            if (afterActionWinner is not null || IsMaxRoundsReached())
            {
                _state.WinnerTeamId = afterActionWinner;
                return Result<PhaseTransition>.Success(PhaseTransition.Next(BattlePhaseIds.End));
            }
        }
    }

    public Result<PhaseTransition?> OnCommand(GameContext context, IGameCommand command)
    {
        if (command is UseActionCommand useAction)
            return OnPlayerAction(context, useAction.ActorRuntimeId, useAction);
        if (command is SkipTurnCommand skipTurn)
            return OnPlayerAction(context, skipTurn.ActorRuntimeId, null);

        return Result<PhaseTransition?>.Success(null);
    }

    private Result<PhaseTransition?> OnPlayerAction(GameContext context, string actorRuntimeId, UseActionCommand? action)
    {
        if (_state.WinnerTeamId is not null)
            return Result<PhaseTransition?>.Success(PhaseTransition.Next(BattlePhaseIds.End));

        var currentId = CurrentActorId();
        if (currentId is null)
            return Result<PhaseTransition?>.Failure($"No actor is taking a turn.");

        if (actorRuntimeId != currentId)
            return Result<PhaseTransition?>.Failure(
                $"Command is not for the current actor. Expected '{currentId}', got '{actorRuntimeId}'.");

        if (action is not null)
        {
            var executed = _executor.Execute(action);
            if (executed.IsFailure)
                return Result<PhaseTransition?>.Failure(executed.Error!);
        }
        else
        {
            context.Visuals.Enqueue(new VisualEvent
            {
                EventType = "TurnSkipped",
                SourceRuntimeId = currentId,
                TargetRuntimeId = currentId,
            });
            context.Log.Append($"SkipTurn: {currentId}");
        }

        _state.TurnIndex++;

        var winner = _rules.WinCondition.WinnerTeamId(_state);
        if (winner is not null || IsMaxRoundsReached())
        {
            _state.WinnerTeamId = winner;
            return Result<PhaseTransition?>.Success(PhaseTransition.Next(BattlePhaseIds.End));
        }

        return CurrentActorId() is null
            ? Result<PhaseTransition?>.Success(PhaseTransition.Next(BattlePhaseIds.RoundStart))
            : Result<PhaseTransition?>.Success(PhaseTransition.Resume());
    }

    private string? CurrentActorId()
    {
        while (_state.TurnIndex < _state.Order.Count)
        {
            var id = _state.Order[_state.TurnIndex];
            if (_state.IsAlive(id))
                return id;
            _state.TurnIndex++;
        }
        return null;
    }

    private static bool HasBlockingStatus(BattleActor actor) => actor.Statuses.Any(s => s.BlocksTurn);

    private bool IsMaxRoundsReached() =>
        _rules.Config.MaxRounds is { } max && _state.RoundNumber >= max;
}
