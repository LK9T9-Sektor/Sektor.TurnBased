using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Effects;

/// <summary>
/// Эффект наложения статуса: создаёт BattleStatus из определения и добавляет на цель.
/// Длительность и модификаторы снимаются с StatusDefinition в момент применения.
/// </summary>
public sealed class ApplyStatusEffect : ICombatEffect
{
    public string Id { get; }
    public string StatusId { get; }

    public ApplyStatusEffect(string id, string statusId)
    {
        Id = id;
        StatusId = statusId;
    }

    public Result Apply(ActionContext context)
    {
        if (!context.Content.TryGet<StatusDefinition>(StatusId, out var definition) || definition is null)
            return Result.Failure($"Status '{StatusId}' is not registered.");

        foreach (var targetId in context.TargetActorIds)
        {
            var target = context.GetActor(targetId);
            if (target is null)
                continue;

            var status = new BattleStatus(
                StatusId,
                definition.Duration,
                context.SourceActorId,
                definition.StatModifiers,
                definition.BlocksTurn,
                definition.TickEffectId);

            var added = target.AddStatus(status);
            if (added.IsSuccess)
                context.Sink?.StatusApplied(targetId, status);
        }
        return Result.Success();
    }
}
