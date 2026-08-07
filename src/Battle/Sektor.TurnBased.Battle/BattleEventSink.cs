using Sektor.TurnBased.Battle.Events;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;

namespace Sektor.TurnBased.Battle;

/// <summary>
/// Реализация ICombatEvents: поднимает события через GameEventBus ядра (для хук-логики)
/// с базовой логикой «визуализация + лог» (для UI). ActorStatChanged и ActorDied идут
/// через шину; остальное — визуал/лог напрямую.
/// </summary>
public sealed class BattleEventSink : ICombatEvents
{
    private readonly GameContext _context;
    private readonly BattleState _state;

    public BattleEventSink(GameContext context, BattleState state)
    {
        _context = context;
        _state = state;
    }

    public void StatChanged(string actorId, StatChange change, string? sourceActorId, bool isCritical = false)
    {
        _context.Events.Raise(
            new ActorStatChanged(actorId, change.StatId, change.Delta, change.NewValue, isCritical),
            applyBase: e =>
            {
                _context.Visuals.Enqueue(new VisualEvent
                {
                    EventType = "StatChanged",
                    SourceRuntimeId = actorId,
                    TargetRuntimeId = actorId,
                    Value = change.NewValue,
                    Delta = change.Delta,
                    IsCritical = e.IsCritical,
                    StatId = change.StatId,
                });
                var sign = change.Delta >= 0 ? "+" : "";
                _context.Log.Append($"{actorId} {change.StatId} {sign}{change.Delta} -> {change.NewValue}");
            });
    }

    public void ActorDied(string actorId, string? sourceActorId)
    {
        _context.Events.Raise(
            new ActorDied(actorId, sourceActorId),
            applyBase: e =>
            {
                _context.Visuals.Enqueue(new VisualEvent
                {
                    EventType = "Died",
                    SourceRuntimeId = sourceActorId ?? actorId,
                    TargetRuntimeId = actorId,
                });
                _context.Log.Append($"{actorId} died");
            });
    }

    public void StatusApplied(string actorId, BattleStatus status)
    {
        _context.Visuals.Enqueue(new VisualEvent
        {
            EventType = "StatusApply",
            SourceRuntimeId = status.SourceActorId,
            TargetRuntimeId = actorId,
            Value = status.Duration,
            Payload = status.StatusId,
        });
        _context.Log.Append($"{actorId} gained status {status.StatusId} for {status.Duration} turns");
    }

    public void ActorSummoned(BattleActor actor)
    {
        _state.AddActor(actor);
        _context.Visuals.Enqueue(new VisualEvent
        {
            EventType = "Summon",
            SourceRuntimeId = actor.RuntimeId,
            TargetRuntimeId = actor.RuntimeId,
        });
        _context.Log.Append($"{actor.RuntimeId} ({actor.TemplateId}) summoned to team {actor.TeamId}");
    }

    public void TurnBlocked(string actorId)
    {
        _context.Visuals.Enqueue(new VisualEvent
        {
            EventType = "TurnBlocked",
            SourceRuntimeId = actorId,
            TargetRuntimeId = actorId,
        });
        _context.Log.Append($"{actorId} skipped turn (blocked)");
    }
}
