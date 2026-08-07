using Sektor.TurnBased.Core;
using Sektor.TurnBased.Dialog.Commands;
using Sektor.TurnBased.Dialog.Content;
using Sektor.TurnBased.Dialog.Events;
using Sektor.TurnBased.Dialog.Model;
using Sektor.TurnBased.Dialog.Phases;
using Xunit;

namespace Sektor.TurnBased.Dialog.Tests;

/// <summary>
/// Интеграционные тесты диалога: полный проход, ветвления по флагам, вложенный
/// диалог (child pipeline), детерминизм, отмена событий через шину и ошибки команд.
/// </summary>
public class DialogEngineTests
{
    private static readonly (string NodeId, string ChoiceId)[] GoodPath =
    {
        ("intro", "approach"),
        ("talk", "pickpocket"),
        ("guard_check", "enter"),
        ("sub_riddle_root", "guess_time"),
        ("after_riddle", "take"),
    };

    private static readonly (string NodeId, string ChoiceId)[] SubDialogRetryPath =
    {
        ("intro", "approach"),
        ("talk", "pickpocket"),
        ("guard_check", "enter"),
        ("sub_riddle_root", "guess_sun"),
        ("riddle_wrong", "try_again"),
        ("sub_riddle_root", "guess_time"),
        ("after_riddle", "take"),
    };

    [Fact]
    public void FullGoodPath_ReachesTreasureEnd()
    {
        var (content, dialogContent) = TestContent.Build();
        var context = TestContent.CreateContext(content);

        var engine = TestContent.RunToEnd(context, dialogContent, GoodPath);

        Assert.True(engine.IsFinished);
        Assert.False(engine.IsSuspended);
        Assert.Equal("treasure_end", engine.Outcome);
        Assert.True(engine.State.HasFlag("papers_stolen"));
        Assert.True(engine.State.HasFlag("riddle_key"));
    }

    [Fact]
    public void SubDialog_IsRunThroughChildPipeline()
    {
        var (content, dialogContent) = TestContent.Build();
        var context = TestContent.CreateContext(content);

        var engine = TestContent.RunToEnd(context, dialogContent, GoodPath);

        Assert.Single(context.Log.Entries, e => e == "Sub-dialog 'sub_riddle_root' entered");
        Assert.Single(context.Log.Entries, e => e == "Sub-dialog 'sub_riddle_root' completed");
        Assert.Contains(context.Log.Entries, e => e == "[sub_riddle_root] Сфинкс: 'Что растёт без корней?'");
        Assert.Equal("treasure_end", engine.Outcome);
    }

    [Fact]
    public void SubDialogRetry_EventuallySucceeds()
    {
        var (content, dialogContent) = TestContent.Build();
        var context = TestContent.CreateContext(content);

        var engine = TestContent.RunToEnd(context, dialogContent, SubDialogRetryPath);

        Assert.True(engine.IsFinished);
        Assert.Equal("treasure_end", engine.Outcome);
        Assert.Contains("riddle_wrong", engine.State.VisitedNodes);
    }

    [Fact]
    public void FightPath_EndsInFightEnd()
    {
        var (content, dialogContent) = TestContent.Build();
        var context = TestContent.CreateContext(content);

        var engine = TestContent.RunToEnd(context, dialogContent,
            ("intro", "approach"), ("talk", "persuade"), ("persuade_try", "fight"));

        Assert.True(engine.IsFinished);
        Assert.Equal("fight_end", engine.Outcome);
        Assert.False(engine.State.HasFlag("papers_stolen"));
    }

    [Fact]
    public void NodeFlagGate_RejectsMissingFlag()
    {
        var (content, dialogContent) = TestContent.Build();
        var context = TestContent.CreateContext(content);
        var engine = TestContent.CreateEngine(context, dialogContent);
        Assert.True(engine.Start().IsSuccess);

        DriveTo(engine, ("intro", "approach"), ("talk", "persuade"));

        var sneak = engine.ProcessCommand(new ChooseOptionCommand("persuade_try", "sneak_past"));
        Assert.True(sneak.IsSuccess);
        Assert.True(engine.Advance().IsFailure);
    }

    [Fact]
    public void ChoiceFlagGate_RejectsMissingFlag()
    {
        var (content, dialogContent) = TestContent.Build();
        var context = TestContent.CreateContext(content);
        var engine = TestContent.CreateEngine(context, dialogContent);
        Assert.True(engine.Start().IsSuccess);

        DriveTo(engine, ("intro", "approach"), ("talk", "persuade"));

        var threaten = engine.ProcessCommand(new ChooseOptionCommand("persuade_try", "threaten"));
        Assert.True(threaten.IsFailure);
        Assert.Contains("requires unavailable flags", threaten.Error);
    }

    [Fact]
    public void CommandForWrongNode_IsRejected()
    {
        var (content, dialogContent) = TestContent.Build();
        var context = TestContent.CreateContext(content);
        var engine = TestContent.CreateEngine(context, dialogContent);
        Assert.True(engine.Start().IsSuccess);

        DriveTo(engine, ("intro", "approach"));

        var wrong = engine.ProcessCommand(new ChooseOptionCommand("persuade_try", "fight"));
        Assert.True(wrong.IsFailure);
        Assert.Contains("not for the current node", wrong.Error);

        var right = engine.ProcessCommand(new ChooseOptionCommand("talk", "persuade"));
        Assert.True(right.IsSuccess);
    }

    [Fact]
    public void UnknownChoice_IsRejected()
    {
        var (content, dialogContent) = TestContent.Build();
        var context = TestContent.CreateContext(content);
        var engine = TestContent.CreateEngine(context, dialogContent);
        Assert.True(engine.Start().IsSuccess);

        DriveTo(engine, ("intro", "approach"));

        var wrong = engine.ProcessCommand(new ChooseOptionCommand("talk", "nope"));
        Assert.True(wrong.IsFailure);
        Assert.Contains("Unknown choice", wrong.Error);
    }

    [Fact]
    public void SameSeed_ProducesIdenticalLogAndVisuals()
    {
        var run1 = RunDeterministic(seed: 7);
        var run2 = RunDeterministic(seed: 7);

        Assert.Equal(run1.Log, run2.Log);
        Assert.Equal(run1.Visuals, run2.Visuals);
    }

    [Fact]
    public void CancelledNodeShown_DoesNotReachVisualsButFlowContinues()
    {
        var (content, dialogContent) = TestContent.Build();
        var context = TestContent.CreateContext(content);
        context.Events.SubscribeBefore<NodeShown>(ctx =>
        {
            if (ctx.Event.NodeId == "talk")
                ctx.IsCancelled = true;
        });

        var engine = TestContent.RunToEnd(context, dialogContent, GoodPath);

        Assert.Equal("treasure_end", engine.Outcome);
        Assert.DoesNotContain(
            DrainVisuals(context.Visuals),
            v => v.EventType == "NodeText" && v.Source == "talk");
    }

    [Fact]
    public void InvalidContent_IsRejectedAtCreate()
    {
        var badNode = new DialogNodeDefinition(
            "a", "Text",
            new[] { new DialogChoiceDefinition("c1", "Text", NextNodeId: "missing", Array.Empty<string>(), Array.Empty<string>()) },
            Array.Empty<string>(), Array.Empty<string>());
        var badContent = new DialogContent(new[] { badNode }, startNodeId: "a", new[] { "flag_a" });
        var (content, _) = TestContent.Build();
        var context = TestContent.CreateContext(content);

        var result = DialogEngine.Create(context, content, badContent);

        Assert.True(result.IsFailure);
        Assert.Contains("unknown node 'missing'", result.Error);
    }

    private static void DriveTo(DialogEngine engine, params (string NodeId, string ChoiceId)[] script)
    {
        foreach (var (nodeId, choiceId) in script)
        {
            while (!TestContent.IsAwaitingChoice(engine))
                Assert.True(engine.Advance().IsSuccess);

            var command = engine.ProcessCommand(new ChooseOptionCommand(nodeId, choiceId));
            Assert.True(command.IsSuccess, command.Error ?? $"command {nodeId}/{choiceId} failed");
        }
    }

    private static (List<string> Log, List<(string EventType, string Source)> Visuals) RunDeterministic(int seed)
    {
        var (content, dialogContent) = TestContent.Build();
        var context = TestContent.CreateContext(content, seed);

        TestContent.RunToEnd(context, dialogContent, GoodPath);

        var visuals = DrainVisuals(context.Visuals)
            .Select(v => (v.EventType, v.Source))
            .ToList();
        return (context.Log.Entries.ToList(), visuals);
    }

    private static List<(string EventType, string Source)> DrainVisuals(VisualQueue visuals)
    {
        var result = new List<(string EventType, string Source)>();
        while (visuals.TryDequeue(out var evt) && evt is not null)
            result.Add((evt.EventType, evt.SourceRuntimeId));
        return result;
    }
}
