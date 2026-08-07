using Sektor.TurnBased.Battle.Commands;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.UI.Core;
using Xunit;

namespace Sektor.TurnBased.UI.Core.Tests;

/// <summary>
/// Тесты BattleSession (UI-адаптер боя): запуск до хода игрока, снапшоты,
/// исполнение действий, скрипт-прохождение, ошибки команд и детерминизм.
/// </summary>
public class BattleSessionTests
{
    [Fact]
    public void Start_ArrivesAtFirstPlayerTurn_AndExposesActions()
    {
        var (_, session) = TestHelpers.CreateBattle(seed: 42);

        Assert.True(session.Start().IsSuccess);
        Assert.False(session.IsFinished);
        Assert.True(session.NeedsInput);
        Assert.False(session.IsFailed);

        var snap = session.Snapshot();
        Assert.NotNull(snap.CurrentActorId);
        Assert.Equal(2, snap.Actors.Count);
        Assert.Equal(1, snap.Actors.Count(a => a.TeamId == "player"));
        Assert.NotEmpty(snap.AvailableActions);
    }

    [Fact]
    public void Snapshot_ReflectsUnitStatsAndVitalStat()
    {
        var (_, session) = TestHelpers.CreateBattle(seed: 42);
        session.Start();

        var snap = session.Snapshot();
        var hero = snap.Actors.First(a => a.ControlledBy == "player");
        Assert.Equal("hero_warrior", hero.TemplateId);
        Assert.True(hero.IsAlive);
        Assert.NotNull(hero.VitalStat);
        Assert.Equal("health", hero.VitalStat!.StatId);
        Assert.Contains(hero.Stats, s => s.StatId == "health" && s.Current > 0 && s.Max is not null);
    }

    [Fact]
    public void SubmitAction_AdvancesAndDrainsVisuals()
    {
        var (_, session) = TestHelpers.CreateBattle(seed: 42);
        session.Start();
        Assert.NotEmpty(session.TakeVisuals());

        var snap = session.Snapshot();
        var goblin = snap.Actors.First(a => a.TeamId != "player");
        var action = snap.AvailableActions.First(a => a.TargetMode == BattleTargetModes.SingleEnemy);
        var submit = session.Submit(new UseActionCommand(snap.CurrentActorId!, action.ActionId, new[] { goblin.RuntimeId }));

        Assert.True(submit.IsSuccess, submit.Error);
        Assert.NotEmpty(session.TakeVisuals());
        Assert.Contains(session.Log, e => e.Contains("basic_attack"));
    }

    [Fact]
    public void FullScript_PlayerWins()
    {
        var (_, session) = TestHelpers.CreateBattle(seed: 42);
        Assert.True(session.Start().IsSuccess);

        RunBattle(session);

        Assert.False(session.IsFailed);
        Assert.True(session.IsFinished);
        Assert.Equal("player", session.Snapshot().WinnerTeamId);
    }

    [Fact]
    public void WrongActorCommand_FailsSession()
    {
        var (_, session) = TestHelpers.CreateBattle(seed: 42);
        session.Start();

        var snap = session.Snapshot();
        var enemy = snap.Actors.First(a => a.TeamId != "player");
        var action = snap.AvailableActions.First();
        var submit = session.Submit(new UseActionCommand(enemy.RuntimeId, action.ActionId, new[] { enemy.RuntimeId }));

        Assert.True(submit.IsFailure);
        Assert.True(session.IsFailed);
        Assert.NotNull(session.Error);
    }

    [Fact]
    public void SubmitSkipTurn_AdvancesTurnAndEmitsVisual()
    {
        var (_, session) = TestHelpers.CreateBattle(seed: 42);
        session.Start();

        var snap = session.Snapshot();
        var submit = session.Submit(new SkipTurnCommand(snap.CurrentActorId!));

        Assert.True(submit.IsSuccess, submit.Error);
        Assert.Contains(session.TakeVisuals(), v => v.EventType == "TurnSkipped");
        Assert.Contains(session.Log, e => e == "SkipTurn: hero_warrior_0");
        Assert.False(session.IsFailed);
    }

    [Fact]
    public void SubmitSkipTurn_ForWrongActor_FailsSession()
    {
        var (_, session) = TestHelpers.CreateBattle(seed: 42);
        session.Start();

        var snap = session.Snapshot();
        var enemy = snap.Actors.First(a => a.TeamId != "player");
        var submit = session.Submit(new SkipTurnCommand(enemy.RuntimeId));

        Assert.True(submit.IsFailure);
        Assert.True(session.IsFailed);
    }

    [Fact]
    public void SameSeed_ProducesIdenticalLogAndVisuals()
    {
        var run1 = PlayBattle(seed: 42);
        var run2 = PlayBattle(seed: 42);

        Assert.Equal(run1.Log, run2.Log);
        Assert.Equal(run1.Visuals, run2.Visuals);
    }

    /// <summary>Скрипт: бьём слабейшего живого врага обычным ударом до конца боя.</summary>
    private static void RunBattle(BattleSession session)
    {
        while (!session.IsFinished && !session.IsFailed)
        {
            if (session.NeedsInput)
            {
                var snap = session.Snapshot();
                var actor = snap.Actors.First(a => a.RuntimeId == snap.CurrentActorId);
                var target = snap.Actors.First(a => a.TeamId != actor.TeamId && a.IsAlive);
                var action = snap.AvailableActions.First(a => a.TargetMode == BattleTargetModes.SingleEnemy);
                if (session.Submit(new UseActionCommand(snap.CurrentActorId!, action.ActionId, new[] { target.RuntimeId })).IsFailure)
                    return;
            }
            else if (session.Advance().IsFailure)
            {
                return;
            }
        }
    }

    [Fact]
    public void Snapshot_ExposesActionGlyphAndDescription()
    {
        var (_, session) = TestHelpers.CreateBattle(seed: 42);
        session.Start();

        var snap = session.Snapshot();
        var attack = snap.AvailableActions.First(a => a.ActionId == "basic_attack");

        Assert.Equal("⚔", attack.Glyph);
        Assert.False(string.IsNullOrWhiteSpace(attack.Description));
    }

    /// <summary>Прогоняет бой и собирает лог и визуальные события для сравнения.</summary>
    private static (List<string> Log, List<(string Type, string Source, string? Target, int Value)> Visuals) PlayBattle(int seed)
    {
        var (_, session) = TestHelpers.CreateBattle(seed);
        session.Start();
        var collected = new List<(string, string, string?, int)>();

        while (!session.IsFinished && !session.IsFailed)
        {
            if (session.NeedsInput)
            {
                var snap = session.Snapshot();
                var actor = snap.Actors.First(a => a.RuntimeId == snap.CurrentActorId);
                var target = snap.Actors.First(a => a.TeamId != actor.TeamId && a.IsAlive);
                var action = snap.AvailableActions.First(a => a.TargetMode == BattleTargetModes.SingleEnemy);
                if (session.Submit(new UseActionCommand(snap.CurrentActorId!, action.ActionId, new[] { target.RuntimeId })).IsFailure)
                    break;
            }
            else if (session.Advance().IsFailure)
            {
                break;
            }

            foreach (var visual in session.TakeVisuals())
                collected.Add((visual.EventType, visual.SourceRuntimeId, visual.TargetRuntimeId, visual.Value));
        }

        return (session.Log.ToList(), collected);
    }
}
