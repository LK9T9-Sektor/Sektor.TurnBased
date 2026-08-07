using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Effects;

/// <summary>
/// Эффект лечения: увеличивает стат, но не выше максимального значения.
/// Семантический эффект (не убивает и не имеет верхнего кламп-ловушки ModifyStat).
/// </summary>
public sealed class HealEffect : ICombatEffect
{
    public string Id { get; }
    public string TargetStatId { get; }
    public int Amount { get; }
    public string? SourceStatId { get; }

    public HealEffect(string id, string targetStatId, int amount = 0, string? sourceStatId = null)
    {
        Id = id;
        TargetStatId = targetStatId;
        Amount = amount;
        SourceStatId = sourceStatId;
    }

    public Result Apply(ActionContext context)
    {
        foreach (var targetId in context.TargetActorIds)
        {
            var target = context.GetActor(targetId);
            if (target is null)
                continue;

            var amount = SourceStatId is not null ? context.EffectiveStat(context.SourceActorId, SourceStatId) : Amount;
            var change = target.Resources.Heal(TargetStatId, amount);
            if (change is not null)
                context.Sink?.StatChanged(targetId, change, context.SourceActorId);
        }
        return Result.Success();
    }
}
