using Sektor.TurnBased.Battle.Commands;
using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Phases;
using Sektor.TurnBased.Battle.Rules;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Xunit;

namespace Sektor.TurnBased.Battle.Tests;

/// <summary>
/// Интеграционные тесты: полный бой через пайплайн ядра, детерминизм и лимит раундов.
/// </summary>
public class BattleIntegrationTests
{
    private sealed class EmptyState : IGameState
    {
    }

    [Fact]
    public void HeroDefeatsGoblin_FullFlow()
    {
        var (content, battleContent) = TestContent.Build();
        var context = new GameContext(new EmptyState(), rng: new DeterministicRng(42), content: content);
        var filtered = TestContent.WithTemplates(battleContent, "hero_warrior", "goblin");

        var engine = RunBattle(
            context,
            content,
            filtered,
            new BattleConfig("initiative", "extermination"),
            new UseActionCommand("hero_warrior_0", "basic_attack", new[] { "goblin_1" }));

        Assert.True(engine.IsFinished);
        Assert.False(engine.IsSuspended);
        Assert.Equal("player", engine.State.WinnerTeamId);

        Assert.Contains(context.Log.Entries, e => e == "Round 1 started");
        Assert.Contains(context.Log.Entries, e => e == "goblin_1 health -12 -> 18");
        Assert.Contains(context.Log.Entries, e => e == "goblin_1 died");
        Assert.Contains(context.Log.Entries, e => e == "Battle ended: winner is player");
    }

    [Fact]
    public void SameSeed_ProducesIdenticalLogAndVisuals()
    {
        var run1 = RunDeterministic(seed: 42);
        var run2 = RunDeterministic(seed: 42);

        Assert.Equal(run1.Log, run2.Log);
        Assert.Equal(run1.Visuals, run2.Visuals);
    }

    [Fact]
    public void MaxRounds_EndsInDraw()
    {
        var (content, battleContent) = TestContent.Build();
        var context = new GameContext(new EmptyState(), rng: new DeterministicRng(42), content: content);
        var filtered = TestContent.WithTemplates(battleContent, "hero_warrior", "goblin");

        var engine = RunBattle(
            context,
            content,
            filtered,
            new BattleConfig("initiative", "extermination", MaxRounds: 2),
            new UseActionCommand("hero_warrior_0", "basic_attack", new[] { "goblin_1" }));

        Assert.True(engine.IsFinished);
        Assert.Null(engine.State.WinnerTeamId);
        Assert.Contains(context.Log.Entries, e => e == "Battle ended: draw");
    }

    [Fact]
    public void CommandForWrongActor_IsRejected()
    {
        var (content, battleContent) = TestContent.Build();
        var context = new GameContext(new EmptyState(), rng: new DeterministicRng(42), content: content);
        var filtered = TestContent.WithTemplates(battleContent, "hero_warrior", "goblin");
        var engineResult = BattleEngine.Create(context, content, filtered, new BattleConfig("initiative", "extermination"));
        Assert.True(engineResult.IsSuccess);
        var engine = engineResult.Value!;

        Assert.True(engine.Start().IsSuccess);
        while (engine.IsSuspended is false && engine.IsFinished is false)
            Assert.True(engine.Advance().IsSuccess);

        Assert.True(engine.IsSuspended);
        var wrong = engine.ProcessCommand(new UseActionCommand("goblin_1", "basic_attack", new[] { "hero_warrior_0" }));
        Assert.True(wrong.IsFailure);

        var right = engine.ProcessCommand(new UseActionCommand("hero_warrior_0", "basic_attack", new[] { "goblin_1" }));
        Assert.True(right.IsSuccess);
        Assert.False(engine.IsSuspended);
    }

    private static (List<string> Log, List<(string EventType, string Source, string? Target, int Value)> Visuals) RunDeterministic(int seed)
    {
        var (content, battleContent) = TestContent.Build();
        var context = new GameContext(new EmptyState(), rng: new DeterministicRng(seed), content: content);
        var filtered = TestContent.WithTemplates(battleContent, "hero_warrior", "goblin");

        RunBattle(
            context,
            content,
            filtered,
            new BattleConfig("initiative", "extermination"),
            new UseActionCommand("hero_warrior_0", "basic_attack", new[] { "goblin_1" }));

        return (context.Log.Entries.ToList(), DrainVisuals(context.Visuals));
    }

    private static List<(string EventType, string Source, string? Target, int Value)> DrainVisuals(VisualQueue visuals)
    {
        var result = new List<(string EventType, string Source, string? Target, int Value)>();
        while (visuals.TryDequeue(out var evt) && evt is not null)
            result.Add((evt.EventType, evt.SourceRuntimeId, evt.TargetRuntimeId, evt.Value));
        return result;
    }

    private static BattleEngine RunBattle(
        GameContext context,
        ContentRegistry content,
        BattleContent battleContent,
        BattleConfig config,
        UseActionCommand playerCommand)
    {
        var engineResult = BattleEngine.Create(context, content, battleContent, config);
        Assert.True(engineResult.IsSuccess);
        var engine = engineResult.Value!;
        Assert.True(engine.Start().IsSuccess);

        while (!engine.IsFinished)
        {
            var advance = engine.Advance();
            Assert.True(advance.IsSuccess, advance.Error ?? "advance failed");

            if (engine.IsSuspended && engine.CurrentPhaseId == BattlePhaseIds.ActorTurn)
            {
                var command = engine.ProcessCommand(playerCommand);
                Assert.True(command.IsSuccess, command.Error ?? "command failed");
            }
        }

        return engine;
    }
}
