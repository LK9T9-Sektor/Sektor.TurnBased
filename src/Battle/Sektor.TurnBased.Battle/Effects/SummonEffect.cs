using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Effects;

/// <summary>
/// Эффект призыва: создаёт актора из шаблона в команду источника.
/// Добавление в состояние и оповещение выполняет sink.
/// </summary>
public sealed class SummonEffect : ICombatEffect
{
    public string Id { get; }
    public string TemplateId { get; }

    public SummonEffect(string id, string templateId)
    {
        Id = id;
        TemplateId = templateId;
    }

    public Result Apply(ActionContext context)
    {
        if (!context.Content.TryGet<ActorTemplateDefinition>(TemplateId, out var template) || template is null)
            return Result.Failure($"Template '{TemplateId}' is not registered.");

        var source = context.GetActor(context.SourceActorId);
        if (source is null)
            return Result.Failure("Source actor not found.");

        var resources = new ResourceContainer(context.State.Definitions);
        foreach (var pair in template.BaseStats)
        {
            var result = resources.SetInitial(pair.Key, pair.Value);
            if (result.IsFailure)
                return Result.Failure(result.Error!);
        }

        var actor = new BattleActor(
            context.State.NewActorId(template.Id),
            source.TeamId,
            template.Id,
            template.ControlledBy,
            resources);

        context.Sink?.ActorSummoned(actor);
        return Result.Success();
    }
}
