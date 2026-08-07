using Sektor.TurnBased.Battle.Rules;
using Sektor.TurnBased.Core;
using Xunit;

namespace Sektor.TurnBased.Battle.Tests;

/// <summary>Тесты правил: порядок ходов и условия победы.</summary>
public class RulesTests
{
    [Fact]
    public void FixedOrderRule_ReturnsInsertionOrder()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        TestContent.AddActor(state, content, "a", "goblin", "enemy", "ai", ("health", 10));
        TestContent.AddActor(state, content, "b", "goblin", "enemy", "ai", ("health", 10));
        TestContent.AddActor(state, content, "c", "goblin", "enemy", "ai", ("health", 10));

        var order = new FixedOrderRule("fixed").Order(state, new DeterministicRng(1));

        Assert.Equal(new[] { "a", "b", "c" }, order);
    }

    [Fact]
    public void SpeedInitiativeRule_OrdersByInitiativeDescending()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        TestContent.AddActor(state, content, "slow", "goblin", "enemy", "ai", ("health", 10), ("initiative", 5));
        TestContent.AddActor(state, content, "fast", "goblin", "enemy", "ai", ("health", 10), ("initiative", 12));

        var order = new SpeedInitiativeRule("initiative").Order(state, new DeterministicRng(1));

        Assert.Equal(new[] { "fast", "slow" }, order);
    }

    [Fact]
    public void SpeedInitiativeRule_TieBreak_IsDeterministic()
    {
        var (content, battleContent) = TestContent.Build();
        var rng1 = new DeterministicRng(7);
        var rng2 = new DeterministicRng(7);
        var state1 = TestContent.CreateState(battleContent);
        var state2 = TestContent.CreateState(battleContent);
        for (var i = 0; i < 5; i++)
        {
            TestContent.AddActor(state1, content, $"a{i}", "goblin", "enemy", "ai", ("health", 10), ("initiative", 5));
            TestContent.AddActor(state2, content, $"a{i}", "goblin", "enemy", "ai", ("health", 10), ("initiative", 5));
        }

        var rule = new SpeedInitiativeRule("initiative");
        var order1 = rule.Order(state1, rng1);
        var order2 = rule.Order(state2, rng2);

        Assert.Equal(order1, order2);
    }

    [Fact]
    public void TeamAlternationRule_InterleavesTeams()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        TestContent.AddActor(state, content, "p1", "hero_warrior", "player", "player", ("health", 10));
        TestContent.AddActor(state, content, "e1", "goblin", "enemy", "ai", ("health", 10));
        TestContent.AddActor(state, content, "p2", "hero_warrior", "player", "player", ("health", 10));
        TestContent.AddActor(state, content, "e2", "goblin", "enemy", "ai", ("health", 10));

        var order = new TeamAlternationRule("alternation").Order(state, new DeterministicRng(1));

        Assert.Equal(new[] { "p1", "e1", "p2", "e2" }, order);
    }

    [Fact]
    public void Extermination_Winner_WhenOneTeamAlive()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var alive = TestContent.AddActor(state, content, "p1", "hero_warrior", "player", "player", ("health", 10));
        TestContent.AddActor(state, content, "e1", "goblin", "enemy", "ai", ("health", 0));

        var winner = new ExterminationCondition("extermination").WinnerTeamId(state);

        Assert.Equal(alive.TeamId, winner);
    }

    [Fact]
    public void Extermination_Null_WhenBothTeamsAlive()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        TestContent.AddActor(state, content, "p1", "hero_warrior", "player", "player", ("health", 10));
        TestContent.AddActor(state, content, "e1", "goblin", "enemy", "ai", ("health", 10));

        var winner = new ExterminationCondition("extermination").WinnerTeamId(state);

        Assert.Null(winner);
    }

    [Fact]
    public void BattleRules_Create_MissingRule_Fails()
    {
        var result = BattleRules.Create(
            new BattleConfig("missing", "extermination"),
            BattleEngine.DefaultOrderRules(),
            BattleEngine.DefaultWinConditions());

        Assert.True(result.IsFailure);
    }
}
