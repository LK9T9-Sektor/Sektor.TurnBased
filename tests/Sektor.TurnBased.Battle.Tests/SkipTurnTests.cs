using Sektor.TurnBased.Battle.Commands;
using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Phases;
using Sektor.TurnBased.Battle.Rules;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Xunit;

namespace Sektor.TurnBased.Battle.Tests;

/// <summary>
/// Тесты команды «пропустить ход»: продвижение по порядку ходов, визуалы, лог и отказ для чужого актора.
/// </summary>
public class SkipTurnTests
{
    private sealed class EmptyState : IGameState
    {
    }

    [Fact]
    public void SkipTurn_AdvancesToNextActor()
    {
        var (content, battleContent) = TestContent.Build();
        var context = new GameContext(new EmptyState(), rng: new DeterministicRng(42), content: content);
        var filtered = TestContent.WithTemplates(battleContent, "hero_warrior", "skeleton");

        var engineResult = BattleEngine.Create(context, content, filtered, new BattleConfig("initiative", "extermination"));
        Assert.True(engineResult.IsSuccess);
        var engine = engineResult.Value!;
        Assert.True(engine.Start().IsSuccess);
        while (engine.IsSuspended is false && engine.IsFinished is false)
            Assert.True(engine.Advance().IsSuccess);

        Assert.True(engine.IsSuspended);
        var result = engine.ProcessCommand(new SkipTurnCommand("hero_warrior_0"));
        Assert.True(result.IsSuccess);

        Assert.Contains(context.Log.Entries, e => e == "SkipTurn: hero_warrior_0");
        Assert.False(engine.IsFinished);
    }

    [Fact]
    public void SkipTurn_EmitsTurnSkippedVisualEvent()
    {
        var (content, battleContent) = TestContent.Build();
        var context = new GameContext(new EmptyState(), rng: new DeterministicRng(42), content: content);
        var filtered = TestContent.WithTemplates(battleContent, "hero_warrior", "skeleton");

        var engineResult = BattleEngine.Create(context, content, filtered, new BattleConfig("initiative", "extermination"));
        Assert.True(engineResult.IsSuccess);
        var engine = engineResult.Value!;
        Assert.True(engine.Start().IsSuccess);
        while (engine.IsSuspended is false && engine.IsFinished is false)
            Assert.True(engine.Advance().IsSuccess);

        Assert.True(engine.ProcessCommand(new SkipTurnCommand("hero_warrior_0")).IsSuccess);

        var turnSkipped = DrainVisuals(context.Visuals).FirstOrDefault(v => v.EventType == "TurnSkipped");
        Assert.Equal("hero_warrior_0", turnSkipped.Source);
        Assert.Equal("hero_warrior_0", turnSkipped.Target);
    }

    [Fact]
    public void SkipTurn_ForWrongActor_IsRejected()
    {
        var (content, battleContent) = TestContent.Build();
        var context = new GameContext(new EmptyState(), rng: new DeterministicRng(42), content: content);
        var filtered = TestContent.WithTemplates(battleContent, "hero_warrior", "skeleton");

        var engineResult = BattleEngine.Create(context, content, filtered, new BattleConfig("initiative", "extermination"));
        Assert.True(engineResult.IsSuccess);
        var engine = engineResult.Value!;
        Assert.True(engine.Start().IsSuccess);
        while (engine.IsSuspended is false && engine.IsFinished is false)
            Assert.True(engine.Advance().IsSuccess);

        var result = engine.ProcessCommand(new SkipTurnCommand("skeleton_1"));
        Assert.True(result.IsFailure);
        Assert.Contains("skeleton_1", result.Error);
    }

    private static List<(string EventType, string Source, string? Target, int Value)> DrainVisuals(VisualQueue visuals)
    {
        var result = new List<(string EventType, string Source, string? Target, int Value)>();
        while (visuals.TryDequeue(out var evt) && evt is not null)
            result.Add((evt.EventType, evt.SourceRuntimeId, evt.TargetRuntimeId, evt.Value));
        return result;
    }
}
