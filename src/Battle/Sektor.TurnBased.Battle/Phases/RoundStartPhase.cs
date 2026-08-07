using Sektor.TurnBased.Battle.Effects;
using Sektor.TurnBased.Battle.Events;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Battle.Rules;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Phases;

/// <summary>
/// Фаза начала раунда: инкремент раунда, тик статусов (включая тик-эффекты), регенерация
/// статов, проверка победы/лимита раундов и пересчёт порядка ходов.
/// </summary>
public sealed class RoundStartPhase : IGamePhase
{
    private readonly BattleState _state;
    private readonly BattleRules _rules;
    private readonly BattleEventSink _sink;

    public string Id => BattlePhaseIds.RoundStart;

    public RoundStartPhase(BattleState state, BattleRules rules, BattleEventSink sink)
    {
        _state = state;
        _rules = rules;
        _sink = sink;
    }

    public Result<PhaseTransition> Execute(GameContext context)
    {
        _state.RoundNumber++;
        var aliveBefore = _state.Actors
            .Where(a => _state.IsAlive(a.RuntimeId))
            .Select(a => a.RuntimeId)
            .ToHashSet();

        foreach (var actor in _state.Actors.ToList())
        {
            TickStatuses(context, actor);
            ApplyTurnRegen(actor);
        }

        foreach (var actor in _state.Actors)
        {
            if (aliveBefore.Contains(actor.RuntimeId) && !_state.IsAlive(actor.RuntimeId))
                _sink.ActorDied(actor.RuntimeId, null);
        }

        var winner = _rules.WinCondition.WinnerTeamId(_state);
        if (winner is not null)
        {
            _state.WinnerTeamId = winner;
            return Result<PhaseTransition>.Success(PhaseTransition.Next(BattlePhaseIds.End));
        }

        if (_rules.Config.MaxRounds is { } max && _state.RoundNumber >= max)
        {
            _state.WinnerTeamId = null;
            return Result<PhaseTransition>.Success(PhaseTransition.Next(BattlePhaseIds.End));
        }

        _state.Order = _rules.OrderRule.Order(_state, context.Rng);
        _state.TurnIndex = 0;

        var order = _state.Order;
        context.Events.Raise(
            new RoundStarted(_state.RoundNumber, order),
            applyBase: e =>
            {
                context.Visuals.Enqueue(new VisualEvent
                {
                    EventType = "RoundStart",
                    SourceRuntimeId = string.Empty,
                    TargetRuntimeId = string.Empty,
                    Value = e.RoundNumber,
                    Payload = e.Order,
                });
                context.Log.Append($"Round {e.RoundNumber} started");
            });

        return Result<PhaseTransition>.Success(PhaseTransition.Next(BattlePhaseIds.ActorTurn));
    }

    private void TickStatuses(GameContext context, BattleActor actor)
    {
        foreach (var status in actor.Statuses.ToList())
        {
            if (status.TickEffectId is not null &&
                context.Content.TryGet<ICombatEffect>(status.TickEffectId, out var tickEffect) &&
                tickEffect is not null)
            {
                var tickContext = new ActionContext(
                    actor.RuntimeId,
                    new[] { actor.RuntimeId },
                    context.Rng,
                    context.Content,
                    _state,
                    _sink);
                tickEffect.Apply(tickContext);
            }

            status.Tick();
        }

        actor.RemoveExpiredStatuses();
    }

    private void ApplyTurnRegen(BattleActor actor)
    {
        foreach (var definition in _state.Definitions.Values)
        {
            if (definition.TurnRegen is not { } regen)
                continue;

            var change = actor.Resources.ModifyStat(definition.Id, regen);
            if (change is not null)
                _sink.StatChanged(actor.RuntimeId, change, actor.RuntimeId);
        }
    }
}
