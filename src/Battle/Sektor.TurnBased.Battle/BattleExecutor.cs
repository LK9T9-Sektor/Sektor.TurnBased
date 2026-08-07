using Sektor.TurnBased.Battle.Commands;
using Sektor.TurnBased.Battle.Effects;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle;

/// <summary>
/// Исполнитель боевых действий: резолвит цели по режиму, прогоняет упорядоченную
/// цепочку прекондиций (все должны пройти) и применяет эффекты по порядку.
/// Контролирует глубину вложенного исполнения и поднимает события о смерти целей.
/// </summary>
public sealed class BattleExecutor
{
    private readonly GameContext _context;
    private readonly BattleState _state;
    private readonly BattleEventSink _sink;
    private readonly int _maxChainDepth;
    private int _executionDepth;

    public BattleExecutor(
        GameContext context,
        BattleState state,
        BattleEventSink sink,
        int maxChainDepth = 16)
    {
        _context = context;
        _state = state;
        _sink = sink;
        _maxChainDepth = maxChainDepth;
    }

    public Result Execute(UseActionCommand command)
    {
        var actor = _state.GetActor(command.ActorRuntimeId);
        if (actor is null)
            return Result.Failure($"Actor '{command.ActorRuntimeId}' is not found.");
        if (!_state.IsAlive(actor.RuntimeId))
            return Result.Failure($"Actor '{actor.RuntimeId}' is dead.");

        if (!_context.Content.TryGet<ActionDefinition>(command.ActionId, out var action) || action is null)
            return Result.Failure($"Action '{command.ActionId}' is not registered.");

        if (!_context.Content.TryGet<ActorTemplateDefinition>(actor.TemplateId, out var template) || template is null)
            return Result.Failure($"Template '{actor.TemplateId}' is not registered.");
        if (!template.ActionIds.Contains(command.ActionId))
            return Result.Failure($"Action '{command.ActionId}' is not available to template '{actor.TemplateId}'.");

        if (_executionDepth >= _maxChainDepth)
            return Result.Failure("Action chain is too deep.");

        var targets = ResolveTargets(actor, action, command);
        if (targets.IsFailure)
            return Result.Failure(targets.Error!);

        _executionDepth++;
        try
        {
            return ExecuteCore(actor, action, targets.Value!);
        }
        finally
        {
            _executionDepth--;
        }
    }

    private Result ExecuteCore(BattleActor actor, ActionDefinition action, IReadOnlyList<string> targetActorIds)
    {
        var context = new ActionContext(
            actor.RuntimeId,
            targetActorIds,
            _context.Rng,
            _context.Content,
            _state,
            _sink);

        foreach (var preconditionId in action.Preconditions)
        {
            if (!_context.Content.TryGet<ICombatPrecondition>(preconditionId, out var precondition) || precondition is null)
                return Result.Failure($"Precondition '{preconditionId}' is not registered.");

            var canApply = precondition.CanApply(context);
            if (canApply.IsFailure)
                return Result.Failure(canApply.Error!);
            if (!canApply.Value)
                return Result.Failure($"Precondition '{preconditionId}' failed for action '{action.Id}'.");
        }

        _context.Log.Append($"{actor.RuntimeId} uses {action.Id} on [{string.Join(", ", targetActorIds)}]");

        var aliveBefore = targetActorIds.ToDictionary(id => id, _state.IsAlive);

        foreach (var effectId in action.Effects)
        {
            if (!_context.Content.TryGet<ICombatEffect>(effectId, out var effect) || effect is null)
                return Result.Failure($"Effect '{effectId}' is not registered.");

            var applied = effect.Apply(context);
            if (applied.IsFailure)
                return Result.Failure(applied.Error!);
        }

        foreach (var targetId in targetActorIds)
        {
            if (aliveBefore.TryGetValue(targetId, out var wasAlive) && wasAlive && !_state.IsAlive(targetId))
                _sink.ActorDied(targetId, actor.RuntimeId);
        }

        return Result.Success();
    }

    private Result<List<string>> ResolveTargets(BattleActor actor, ActionDefinition action, UseActionCommand command)
    {
        if (action.TargetMode == BattleTargetModes.Self)
            return Result<List<string>>.Success(new List<string> { actor.RuntimeId });

        if (action.TargetMode == BattleTargetModes.AllEnemies)
        {
            var enemies = _state.AliveActors()
                .Where(a => a.TeamId != actor.TeamId)
                .Select(a => a.RuntimeId)
                .ToList();
            return enemies.Count > 0
                ? Result<List<string>>.Success(enemies)
                : Result<List<string>>.Failure("No valid targets for action.");
        }

        if (action.TargetMode == BattleTargetModes.SingleEnemy)
        {
            if (command.TargetActorIds.Count != 1)
                return Result<List<string>>.Failure("Single-enemy action requires exactly one target.");

            var targetId = command.TargetActorIds[0];
            var target = _state.GetActor(targetId);
            if (target is null || !_state.IsAlive(targetId))
                return Result<List<string>>.Failure($"Target '{targetId}' is not alive.");
            if (target.TeamId == actor.TeamId)
                return Result<List<string>>.Failure("Target must be an enemy.");

            return Result<List<string>>.Success(new List<string> { targetId });
        }

        return Result<List<string>>.Failure($"Unknown target mode '{action.TargetMode}'.");
    }
}
