using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Rules;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.Dialog.Content;
using Sektor.TurnBased.UI.Core;

namespace Sektor.TurnBased.UI.Core.Tests;

/// <summary>
/// Хелперы тестов: строят демо-контент и создают сессии (бой — с фильтром
/// «герой + гоблин» для простых детерминированных сценариев).
/// </summary>
internal static class TestHelpers
{
    private sealed class EmptyState : IGameState
    {
    }

    /// <summary>Создаёт бой «воин против гоблина» с детерминированным seed.</summary>
    public static (ContentRegistry Content, BattleSession Session) CreateBattle(int seed = 42)
    {
        var content = new ContentRegistry();
        var build = BattleContentCatalog.Build(content);
        if (build.IsFailure)
            throw new InvalidOperationException($"Battle content failed: {build.Error}");

        var filtered = FilterTemplates(build.Value!, "hero_warrior", "skeleton");
        var context = new GameContext(new EmptyState(), rng: new DeterministicRng(seed), content: content);
        var created = BattleSession.Create(context, content, filtered, new BattleConfig("initiative", "extermination"));
        if (created.IsFailure)
            throw new InvalidOperationException($"Battle session failed: {created.Error}");

        return (content, created.Value!);
    }

    /// <summary>Создаёт сессию квеста с детерминированным seed.</summary>
    public static (ContentRegistry Content, DialogContent DialogContent, DialogSession Session) CreateDialog(int seed = 7)
    {
        var content = new ContentRegistry();
        var build = DialogContentCatalog.Build(content);
        if (build.IsFailure)
            throw new InvalidOperationException($"Dialog content failed: {build.Error}");

        var context = new GameContext(new EmptyState(), rng: new DeterministicRng(seed), content: content);
        var created = DialogSession.Create(context, content, build.Value!);
        if (created.IsFailure)
            throw new InvalidOperationException($"Dialog session failed: {created.Error}");

        return (content, build.Value!, created.Value!);
    }

    /// <summary>Оставляет в BattleContent только указанные шаблоны акторов.</summary>
    public static BattleContent FilterTemplates(BattleContent source, params string[] templateIds)
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
