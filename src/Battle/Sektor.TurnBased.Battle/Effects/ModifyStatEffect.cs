using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Effects;

/// <summary>
/// Эффект «сырой» дельты стата (бафф/расход ресурса) без семантики урона/лечения.
/// Клампы — только по определению стата (Max/Min), без привязки к максимуму актора.
/// </summary>
public sealed class ModifyStatEffect : ICombatEffect
{
    public string Id { get; }
    public string StatId { get; }
    public int Amount { get; }

    public ModifyStatEffect(string id, string statId, int amount)
    {
        Id = id;
        StatId = statId;
        Amount = amount;
    }

    public Result Apply(ActionContext context)
    {
        foreach (var targetId in context.TargetActorIds)
        {
            var target = context.GetActor(targetId);
            if (target is null)
                continue;

            var change = target.Resources.ModifyStat(StatId, Amount);
            if (change is not null)
                context.Sink?.StatChanged(targetId, change, context.SourceActorId);
        }
        return Result.Success();
    }
}
