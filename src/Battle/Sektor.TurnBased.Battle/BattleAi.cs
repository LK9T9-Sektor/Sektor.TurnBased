using Sektor.TurnBased.Battle.Commands;
using Sektor.TurnBased.Battle.Effects;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;

namespace Sektor.TurnBased.Battle;

/// <summary>
/// AI врагов: оценивает урон доступных действий (EstimateDamage, пре-коммит, без побочных
/// эффектов) и выбирает максимальный по слабейшей цели. Возвращает тот же UseActionCommand,
/// что и игрок. Детерминированно: при равном уроне предпочитает более слабую цель.
/// </summary>
public sealed class BattleAi
{
    private readonly GameContext _context;
    private readonly BattleState _state;

    public BattleAi(GameContext context, BattleState state)
    {
        _context = context;
        _state = state;
    }

    /// <summary>Выбирает команду для актора или null, если ни одно действие неприменимо.</summary>
    public UseActionCommand? ChooseCommand(string actorId)
    {
        var actor = _state.GetActor(actorId);
        if (actor is null || !_state.IsAlive(actorId))
            return null;

        if (!_context.Content.TryGet<ActorTemplateDefinition>(actor.TemplateId, out var template) || template is null)
            return null;

        UseActionCommand? best = null;
        var bestScore = int.MinValue;
        var bestWeakness = int.MinValue;

        foreach (var actionId in template.ActionIds)
        {
            if (!_context.Content.TryGet<ActionDefinition>(actionId, out var action) || action is null)
                continue;

            foreach (var candidate in Evaluate(actor, action))
            {
                var (score, targets, primaryTargetId) = candidate;
                var weakness = primaryTargetId is not null
                    ? -_state.EffectiveStat(primaryTargetId, HealthStatId)
                    : 0;

                if (score > bestScore || (score == bestScore && weakness > bestWeakness))
                {
                    bestScore = score;
                    bestWeakness = weakness;
                    best = new UseActionCommand(actorId, actionId, targets);
                }
            }
        }

        if (best is not null)
            return best;

        return FirstApplicable(actor, template);
    }

    private string HealthStatId => _state.DeathStatId ?? "health";

    private IEnumerable<(int Score, IReadOnlyList<string> Targets, string? PrimaryTargetId)> Evaluate(
        BattleActor actor,
        ActionDefinition action)
    {
        if (action.TargetMode == BattleTargetModes.Self)
        {
            var context = BuildContext(actor.RuntimeId, new[] { actor.RuntimeId });
            if (PassesPreconditions(context, action))
                yield return (Score(action, context, actor.RuntimeId), new[] { actor.RuntimeId }, actor.RuntimeId);
            yield break;
        }

        var enemies = _state.AliveActors().Where(a => a.TeamId != actor.TeamId).ToList();

        if (action.TargetMode == BattleTargetModes.AllEnemies)
        {
            if (enemies.Count == 0)
                yield break;

            var context = BuildContext(actor.RuntimeId, enemies.Select(e => e.RuntimeId).ToList());
            if (PassesPreconditions(context, action))
            {
                var score = enemies.Sum(e => Score(action, context, e.RuntimeId));
                var weakest = enemies.OrderBy(e => _state.EffectiveStat(e.RuntimeId, HealthStatId)).First();
                yield return (score, enemies.Select(e => e.RuntimeId).ToList(), weakest.RuntimeId);
            }
            yield break;
        }

        if (action.TargetMode == BattleTargetModes.SingleEnemy)
        {
            foreach (var enemy in enemies)
            {
                var context = BuildContext(actor.RuntimeId, new[] { enemy.RuntimeId });
                if (PassesPreconditions(context, action))
                    yield return (Score(action, context, enemy.RuntimeId), new[] { enemy.RuntimeId }, enemy.RuntimeId);
            }
        }
    }

    private int Score(ActionDefinition action, ActionContext context, string targetActorId)
    {
        var total = 0;
        foreach (var effectId in action.Effects)
        {
            if (_context.Content.TryGet<ICombatEffect>(effectId, out var effect) && effect is not null)
                total += effect.EstimateDamage(context, targetActorId);
        }
        return total;
    }

    private bool PassesPreconditions(ActionContext context, ActionDefinition action)
    {
        foreach (var preconditionId in action.Preconditions)
        {
            if (!_context.Content.TryGet<ICombatPrecondition>(preconditionId, out var precondition) || precondition is null)
                return false;

            var canApply = precondition.CanApply(context);
            if (canApply.IsFailure || !canApply.Value)
                return false;
        }
        return true;
    }

    private UseActionCommand? FirstApplicable(BattleActor actor, ActorTemplateDefinition template)
    {
        foreach (var actionId in template.ActionIds)
        {
            if (!_context.Content.TryGet<ActionDefinition>(actionId, out var action) || action is null)
                continue;

            if (action.TargetMode == BattleTargetModes.Self)
            {
                var context = BuildContext(actor.RuntimeId, new[] { actor.RuntimeId });
                if (PassesPreconditions(context, action))
                    return new UseActionCommand(actor.RuntimeId, actionId, new[] { actor.RuntimeId });
            }
            else
            {
                var enemies = _state.AliveActors().Where(a => a.TeamId != actor.TeamId).Select(a => a.RuntimeId).ToList();
                if (action.TargetMode == BattleTargetModes.AllEnemies && enemies.Count > 0)
                {
                    var context = BuildContext(actor.RuntimeId, enemies);
                    if (PassesPreconditions(context, action))
                        return new UseActionCommand(actor.RuntimeId, actionId, enemies);
                }
                else if (action.TargetMode == BattleTargetModes.SingleEnemy && enemies.Count > 0)
                {
                    var target = enemies[0];
                    var context = BuildContext(actor.RuntimeId, new[] { target });
                    if (PassesPreconditions(context, action))
                        return new UseActionCommand(actor.RuntimeId, actionId, new[] { target });
                }
            }
        }
        return null;
    }

    private ActionContext BuildContext(string sourceActorId, IReadOnlyList<string> targetActorIds) =>
        new(sourceActorId, targetActorIds, _context.Rng, _context.Content, _state);
}
