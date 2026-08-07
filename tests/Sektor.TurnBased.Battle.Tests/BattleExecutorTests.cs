using Sektor.TurnBased.Battle.Commands;
using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;
using Xunit;

namespace Sektor.TurnBased.Battle.Tests;

/// <summary>Тесты исполнителя боевых действий.</summary>
public class BattleExecutorTests
{
    private static (BattleState State, BattleExecutor Executor, GameContext Context) Create()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var context = new GameContext(state, rng: new DeterministicRng(1), content: content);
        var sink = new BattleEventSink(context, state);
        var executor = new BattleExecutor(context, state, sink);
        return (state, executor, context);
    }

    private static UseActionCommand Attack(string actorId, string targetId, string actionId = "basic_attack") =>
        new(actorId, actionId, new[] { targetId });

    [Fact]
    public void Execute_ValidCommand_AppliesDamage()
    {
        var (state, executor, context) = Create();
        var hero = TestContent.AddActor(state, context.Content, "hero", "hero_warrior", "player", "player", ("attack", 12), ("health", 100));
        var goblin = TestContent.AddActor(state, context.Content, "goblin", "goblin", "enemy", "ai", ("health", 30));

        var result = executor.Execute(Attack(hero.RuntimeId, goblin.RuntimeId));

        Assert.True(result.IsSuccess);
        Assert.True(goblin.Resources.TryGetCurrent("health", out var health));
        Assert.Equal(18, health);
        Assert.Contains(context.Log.Entries, e => e.Contains("uses basic_attack"));
    }

    [Fact]
    public void Execute_UnknownActor_Fails()
    {
        var (state, executor, context) = Create();
        var goblin = TestContent.AddActor(state, context.Content, "goblin", "goblin", "enemy", "ai", ("health", 30));
        var result = executor.Execute(Attack("missing", goblin.RuntimeId));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Execute_DeadActor_Fails()
    {
        var (state, executor, context) = Create();
        var hero = TestContent.AddActor(state, context.Content, "hero", "hero_warrior", "player", "player", ("health", 0));
        var goblin = TestContent.AddActor(state, context.Content, "goblin", "goblin", "enemy", "ai", ("health", 30));

        var result = executor.Execute(Attack(hero.RuntimeId, goblin.RuntimeId));

        Assert.True(result.IsFailure);
        Assert.Contains("dead", result.Error!);
    }

    [Fact]
    public void Execute_ActionNotInTemplate_Fails()
    {
        var (state, executor, context) = Create();
        var rogue = TestContent.AddActor(state, context.Content, "rogue", "hero_rogue", "player", "player", ("health", 10));
        var goblin = TestContent.AddActor(state, context.Content, "goblin", "goblin", "enemy", "ai", ("health", 30));

        var result = executor.Execute(Attack(rogue.RuntimeId, goblin.RuntimeId, "battle_rage"));

        Assert.True(result.IsFailure);
        Assert.Contains("not available", result.Error!);
    }

    [Fact]
    public void Execute_DeadTarget_IsRejected()
    {
        var (state, executor, context) = Create();
        var hero = TestContent.AddActor(state, context.Content, "hero", "hero_warrior", "player", "player", ("attack", 12), ("health", 100));
        var dead = TestContent.AddActor(state, context.Content, "goblin", "goblin", "enemy", "ai", ("health", 0));

        var result = executor.Execute(Attack(hero.RuntimeId, dead.RuntimeId));

        Assert.True(result.IsFailure);
        Assert.Contains("not alive", result.Error!);
    }

    [Fact]
    public void Execute_TargetMustBeEnemy_Fails()
    {
        var (state, executor, context) = Create();
        var hero = TestContent.AddActor(state, context.Content, "hero", "hero_warrior", "player", "player", ("attack", 12), ("health", 100));
        var ally = TestContent.AddActor(state, context.Content, "hero2", "hero_warrior", "player", "player", ("health", 30));

        var result = executor.Execute(Attack(hero.RuntimeId, ally.RuntimeId));

        Assert.True(result.IsFailure);
        Assert.Contains("enemy", result.Error!);
    }

    [Fact]
    public void Execute_ActionThatKills_EmitsDeath()
    {
        var (state, executor, context) = Create();
        var hero = TestContent.AddActor(state, context.Content, "hero", "hero_warrior", "player", "player", ("attack", 12), ("health", 100));
        var goblin = TestContent.AddActor(state, context.Content, "goblin", "goblin", "enemy", "ai", ("health", 10));

        var result = executor.Execute(Attack(hero.RuntimeId, goblin.RuntimeId));

        Assert.True(result.IsSuccess);
        Assert.False(state.IsAlive(goblin.RuntimeId));
        Assert.Contains(context.Log.Entries, e => e == "goblin died");
    }

    [Fact]
    public void Execute_EffectsAppliedInOrder()
    {
        var (state, executor, context) = Create();
        var hero = TestContent.AddActor(state, context.Content, "hero", "hero_warrior", "player", "player", ("attack", 12), ("health", 100));

        var result = executor.Execute(new UseActionCommand(hero.RuntimeId, "battle_rage", new[] { hero.RuntimeId }));

        Assert.True(result.IsSuccess);
        Assert.True(hero.Resources.TryGetCurrent("attack", out var baseAttack));
        Assert.Equal(15, baseAttack);
        Assert.Equal(18, state.EffectiveStat(hero.RuntimeId, "attack"));
        Assert.Single(hero.Statuses);
    }
}
