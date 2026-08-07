using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Effects;

/// <summary>
/// Эффект урона: атака (база + стат источника, если задан) минус плоская митигация
/// цели (если задан стат). Митигация — флэт-редукция, а не стат урона.
/// </summary>
public sealed class DamageEffect : ICombatEffect
{
    public string Id { get; }
    public string TargetStatId { get; }
    public int Amount { get; }
    public string? SourceStatId { get; }
    public string? MitigationStatId { get; }

    public DamageEffect(
        string id,
        string targetStatId,
        int amount = 0,
        string? sourceStatId = null,
        string? mitigationStatId = null)
    {
        Id = id;
        TargetStatId = targetStatId;
        Amount = amount;
        SourceStatId = sourceStatId;
        MitigationStatId = mitigationStatId;
    }

    public Result Apply(ActionContext context)
    {
        foreach (var targetId in context.TargetActorIds)
        {
            var damage = ComputeDamage(context, targetId);
            if (damage <= 0)
                continue;

            var target = context.GetActor(targetId);
            if (target is null)
                continue;

            var change = target.Resources.ModifyStat(TargetStatId, -damage);
            if (change is not null)
                context.Sink?.StatChanged(targetId, change, context.SourceActorId);
        }
        return Result.Success();
    }

    public int EstimateDamage(ActionContext context, string targetActorId) => ComputeDamage(context, targetActorId);

    private int ComputeDamage(ActionContext context, string targetActorId)
    {
        var attack = (SourceStatId is not null ? context.EffectiveStat(context.SourceActorId, SourceStatId) : 0) + Amount;
        var mitigation = MitigationStatId is not null ? context.EffectiveStat(targetActorId, MitigationStatId) : 0;
        return Math.Max(0, attack - mitigation);
    }
}
