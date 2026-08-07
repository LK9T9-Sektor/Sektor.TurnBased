using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Effects;

/// <summary>Прекондиция: эффективное значение стата источника не ниже минимума.</summary>
public sealed class HasResourcePrecondition : ICombatPrecondition
{
    public string Id { get; }
    public string StatId { get; }
    public int Minimum { get; }

    public HasResourcePrecondition(string id, string statId, int minimum)
    {
        Id = id;
        StatId = statId;
        Minimum = minimum;
    }

    public Result<bool> CanApply(ActionContext context) =>
        Result<bool>.Success(context.SourceEffectiveStat(StatId) >= Minimum);
}
