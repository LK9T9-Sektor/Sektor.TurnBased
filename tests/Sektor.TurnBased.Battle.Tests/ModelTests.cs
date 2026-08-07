using Sektor.TurnBased.Battle.Model;
using Xunit;

namespace Sektor.TurnBased.Battle.Tests;

/// <summary>Тесты модели: ресурсы, смерть, эффективные статы.</summary>
public class ModelTests
{
    [Fact]
    public void ModifyStat_ClampsMin_ForClampMinStat()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var actor = TestContent.AddActor(state, content, "hero", "hero_warrior", "player", "player", ("health", 10));

        var change = actor.Resources.ModifyStat("health", -20);

        Assert.NotNull(change);
        Assert.Equal(0, change.NewValue);
        Assert.Equal(-10, change.Delta);
    }

    [Fact]
    public void ModifyStat_UnknownStat_ReturnsNull()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var actor = TestContent.AddActor(state, content, "hero", "hero_warrior", "player", "player", ("health", 10));

        var change = actor.Resources.ModifyStat("mana", -1);

        Assert.Null(change);
    }

    [Fact]
    public void ModifyStat_ZeroDelta_ReturnsNull()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var actor = TestContent.AddActor(state, content, "hero", "hero_warrior", "player", "player", ("health", 10));

        var change = actor.Resources.ModifyStat("attack", 0);

        Assert.Null(change);
    }

    [Fact]
    public void Heal_ClampsToMax()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var actor = TestContent.AddActor(state, content, "hero", "hero_warrior", "player", "player", ("health", 30));
        actor.Resources.ModifyStat("health", -10);

        var change = actor.Resources.Heal("health", 100);

        Assert.NotNull(change);
        Assert.Equal(30, change.NewValue);
        Assert.Equal(10, change.Delta);
    }

    [Fact]
    public void IsAlive_IsFalse_WhenHealthAtZero()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var actor = TestContent.AddActor(state, content, "hero", "hero_warrior", "player", "player", ("health", 10));
        actor.Resources.ModifyStat("health", -10);

        Assert.False(state.IsAlive(actor.RuntimeId));
    }

    [Fact]
    public void EffectiveStat_IncludesStatusModifiers()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var actor = TestContent.AddActor(state, content, "hero", "hero_warrior", "player", "player", ("attack", 5));

        actor.AddStatus(new BattleStatus(
            "rage",
            2,
            "hero",
            new Dictionary<string, int> { ["attack"] = 3 },
            blocksTurn: false,
            tickEffectId: null));

        Assert.Equal(8, state.EffectiveStat(actor.RuntimeId, "attack"));
    }

    [Fact]
    public void DeathStatId_ReturnsHealth()
    {
        var (_, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);

        Assert.Equal("health", state.DeathStatId);
    }

    [Fact]
    public void NewActorId_IsUnique()
    {
        var (_, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);

        Assert.NotEqual(state.NewActorId("hero"), state.NewActorId("hero"));
    }
}
