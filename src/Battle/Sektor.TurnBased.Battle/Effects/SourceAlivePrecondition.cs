using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Effects;

/// <summary>Прекондиция: источник действия жив.</summary>
public sealed class SourceAlivePrecondition : ICombatPrecondition
{
    public string Id { get; }

    public SourceAlivePrecondition(string id) => Id = id;

    public Result<bool> CanApply(ActionContext context) =>
        Result<bool>.Success(context.IsAlive(context.SourceActorId));
}
