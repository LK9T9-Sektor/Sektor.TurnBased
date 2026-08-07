using Sektor.TurnBased.Dialog.Commands;
using Sektor.TurnBased.UI.Core;
using Xunit;

namespace Sektor.TurnBased.UI.Core.Tests;

/// <summary>
/// Тесты DialogSession (UI-адаптер квеста): запуск до узла, продвижение выбором,
/// полный проход с под-диалогом к концовке, ошибки неизвестного выбора и детерминизм.
/// </summary>
public class DialogSessionTests
{
    /// <summary>Хороший путь: ворота → под-диалог → сокровище.</summary>
    private static readonly (string NodeId, string ChoiceId)[] GoodPath =
    {
        ("intro", "approach"),
        ("talk", "pickpocket"),
        ("guard_check", "enter"),
        ("sub_riddle_root", "guess_time"),
        ("after_riddle", "take"),
    };

    [Fact]
    public void Start_ShowsIntroNodeWithChoice()
    {
        var (_, _, session) = TestHelpers.CreateDialog(seed: 7);

        Assert.True(session.Start().IsSuccess);
        Assert.True(session.NeedsInput);

        var snap = session.Snapshot();
        Assert.Equal("intro", snap.NodeId);
        Assert.Contains("стражник", snap.NodeText);
        Assert.Single(snap.Choices);
        Assert.Equal("approach", snap.Choices[0].ChoiceId);
    }

    [Fact]
    public void Choose_AdvancesToNextNode()
    {
        var (_, _, session) = TestHelpers.CreateDialog(seed: 7);
        session.Start();

        var submit = session.Submit(new ChooseOptionCommand("intro", "approach"));

        Assert.True(submit.IsSuccess, submit.Error);
        Assert.True(session.NeedsInput);
        var snap = session.Snapshot();
        Assert.Equal("talk", snap.NodeId);
        Assert.Equal(2, snap.Choices.Count);
    }

    [Fact]
    public void FullGoodPath_EndsWithTreasureOutcome()
    {
        var (_, _, session) = TestHelpers.CreateDialog(seed: 7);
        Assert.True(session.Start().IsSuccess);

        foreach (var (nodeId, choiceId) in GoodPath)
        {
            Assert.True(session.NeedsInput, $"expected awaiting input at {nodeId}");
            var snap = session.Snapshot();
            Assert.Equal(nodeId, snap.NodeId);
            var submit = session.Submit(new ChooseOptionCommand(nodeId, choiceId));
            Assert.True(submit.IsSuccess, submit.Error);
        }

        Assert.True(session.IsFinished);
        Assert.False(session.IsFailed);
        Assert.Equal("treasure_end", session.Snapshot().OutcomeNodeId);
    }

    [Fact]
    public void UnknownChoice_FailsSession()
    {
        var (_, _, session) = TestHelpers.CreateDialog(seed: 7);
        session.Start();

        var submit = session.Submit(new ChooseOptionCommand("intro", "no_such_choice"));

        Assert.True(submit.IsFailure);
        Assert.True(session.IsFailed);
    }

    [Fact]
    public void SameSeed_ProducesIdenticalLogAndVisuals()
    {
        var run1 = PlayDialog(seed: 7);
        var run2 = PlayDialog(seed: 7);

        Assert.Equal(run1.Log, run2.Log);
        Assert.Equal(run1.Visuals, run2.Visuals);
    }

    /// <summary>Прогоняет хороший путь и собирает лог и визуальные события.</summary>
    private static (List<string> Log, List<(string Type, string Source)> Visuals) PlayDialog(int seed)
    {
        var (_, _, session) = TestHelpers.CreateDialog(seed);
        session.Start();
        var collected = new List<(string, string)>();

        foreach (var (nodeId, choiceId) in GoodPath)
        {
            if (session.IsFinished || session.IsFailed)
                break;
            if (session.Submit(new ChooseOptionCommand(nodeId, choiceId)).IsFailure)
                break;
            foreach (var visual in session.TakeVisuals())
                collected.Add((visual.EventType, visual.SourceRuntimeId));
        }

        return (session.Log.ToList(), collected);
    }
}
