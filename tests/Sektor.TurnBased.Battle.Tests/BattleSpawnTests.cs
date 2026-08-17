using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Battle.Rules;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Xunit;

namespace Sektor.TurnBased.Battle.Tests;

/// <summary>
/// Тесты ростера боя: явный список спавнов (слоты игроков player_N + AI),
/// фолбэк по умолчанию на все шаблоны каталога и валидация неизвестных шаблонов.
/// </summary>
public class BattleSpawnTests
{
    private sealed class EmptyState : IGameState
    {
    }

    [Fact]
    public void CustomSpawns_CreateActorsWithSlots()
    {
        var (content, battleContent) = TestContent.Build();
        var context = new GameContext(new EmptyState(), rng: new DeterministicRng(42), content: content);

        var spawns = new[]
        {
            new BattleSpawn("hero_warrior", "player", "player_0"),
            new BattleSpawn("hero_rogue", "player", "player_1"),
            new BattleSpawn("skeleton", "enemy", "ai"),
        };
        var created = BattleEngine.Create(
            context, content, battleContent, new BattleConfig("initiative", "extermination"), spawns: spawns);

        Assert.True(created.IsSuccess, created.Error);
        var engine = created.Value!;
        Assert.True(engine.Start().IsSuccess);
        Assert.True(engine.Advance().IsSuccess);

        Assert.Equal(3, engine.State.Actors.Count);
        Assert.Contains(engine.State.Actors, a => a.TemplateId == "hero_warrior" && a.ControlledBy == "player_0");
        Assert.Contains(engine.State.Actors, a => a.TemplateId == "hero_rogue" && a.ControlledBy == "player_1");
        Assert.Contains(engine.State.Actors, a => a.TemplateId == "skeleton" && a.ControlledBy == "ai");
        Assert.All(engine.State.Actors.Where(a => a.IsHumanControlled), a => Assert.StartsWith("player_", a.ControlledBy));
        Assert.All(engine.State.Actors.Where(a => !a.IsHumanControlled), a => Assert.Equal("ai", a.ControlledBy));
    }

    [Fact]
    public void UnknownTemplateSpawn_FailsAtStart()
    {
        var (content, battleContent) = TestContent.Build();
        var context = new GameContext(new EmptyState(), rng: new DeterministicRng(42), content: content);

        var spawns = new[] { new BattleSpawn("hero_missing", "player", "player_0") };
        var created = BattleEngine.Create(
            context, content, battleContent, new BattleConfig("initiative", "extermination"), spawns: spawns);
        Assert.True(created.IsSuccess, created.Error);

        Assert.True(created.Value!.Start().IsSuccess);
        var result = created.Value!.Advance();
        Assert.True(result.IsFailure);
        Assert.Contains("hero_missing", result.Error);
    }

    [Fact]
    public void DefaultSpawns_UseAllTemplates()
    {
        var (content, battleContent) = TestContent.Build();
        var context = new GameContext(new EmptyState(), rng: new DeterministicRng(42), content: content);

        var created = BattleEngine.Create(context, content, battleContent, new BattleConfig("initiative", "extermination"));
        Assert.True(created.IsSuccess, created.Error);
        var engine = created.Value!;
        Assert.True(engine.Start().IsSuccess);
        Assert.True(engine.Advance().IsSuccess);

        Assert.Equal(battleContent.Templates.Count, engine.State.Actors.Count);
    }
}