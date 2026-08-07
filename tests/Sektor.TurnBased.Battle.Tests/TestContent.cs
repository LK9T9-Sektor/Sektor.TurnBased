using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;

namespace Sektor.TurnBased.Battle.Tests;

/// <summary>
/// Тестовый контент: собирает демо-каталог и упрощает создание состояний и акторов.
/// </summary>
internal static class TestContent
{
    public static (ContentRegistry Content, BattleContent BattleContent) Build()
    {
        var content = new ContentRegistry();
        var result = BattleContentCatalog.Build(content);
        if (result.IsFailure)
            throw new InvalidOperationException($"Test content failed to build: {result.Error}");
        return (content, result.Value!);
    }

    public static BattleState CreateState(BattleContent battleContent) =>
        new(battleContent.Stats.ToDictionary(s => s.Id));

    public static BattleActor AddActor(
        BattleState state,
        ContentRegistry content,
        string runtimeId,
        string templateId,
        string teamId,
        string controlledBy,
        params (string Stat, int Value)[] stats)
    {
        _ = content.Get<ActorTemplateDefinition>(templateId);

        var resources = new ResourceContainer(state.Definitions);
        foreach (var (stat, value) in stats)
        {
            var result = resources.SetInitial(stat, value);
            if (result.IsFailure)
                throw new InvalidOperationException(result.Error);
        }

        var actor = new BattleActor(runtimeId, teamId, templateId, controlledBy, resources);
        state.AddActor(actor);
        return actor;
    }

    public static ActionContext CreateContext(
        BattleState state,
        ContentRegistry content,
        string sourceActorId,
        IReadOnlyList<string> targetActorIds,
        Events.ICombatEvents? sink = null,
        int seed = 1) =>
        new(sourceActorId, targetActorIds, new DeterministicRng(seed), content, state, sink);

    public static BattleContent WithTemplates(BattleContent source, params string[] templateIds)
    {
        var templates = source.Templates.Where(t => templateIds.Contains(t.Id)).ToList();
        return new BattleContent(
            source.Stats,
            source.Statuses,
            source.Actions,
            templates,
            source.Effects,
            source.Preconditions);
    }
}
