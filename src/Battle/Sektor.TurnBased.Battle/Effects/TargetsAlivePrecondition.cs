using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Effects;

/// <summary>Прекондиция: все цели действия живы.</summary>
public sealed class TargetsAlivePrecondition : ICombatPrecondition
{
    public string Id { get; }

    public TargetsAlivePrecondition(string id) => Id = id;

    public Result<bool> CanApply(ActionContext context)
    {
        var allAlive = context.TargetActorIds.Count > 0 &&
                       context.TargetActorIds.All(context.IsAlive);
        return Result<bool>.Success(allAlive);
    }
}
