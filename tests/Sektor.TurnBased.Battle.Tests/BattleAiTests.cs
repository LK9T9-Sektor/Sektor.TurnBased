using Sektor.TurnBased.Battle.Commands;
using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;
using Xunit;

namespace Sektor.TurnBased.Battle.Tests;

/// <summary>Тесты AI врагов: выбор действия и цели по оценке урона.</summary>
public class BattleAiTests
{
    private static (BattleState State, BattleAi Ai, GameContext Context) Create()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var context = new GameContext(state, rng: new DeterministicRng(1), content: content);
        return (state, new BattleAi(context, state), context);
    }

    [Fact]
    public void ChooseCommand_PicksHighestDamageTarget()
    {
        var (state, ai, context) = Create();
        var goblin = TestContent.AddActor(state, context.Content, "goblin", "goblin", "enemy", "ai", ("attack", 7), ("health", 30));
        var armored = TestContent.AddActor(state, context.Content, "warrior", "hero_warrior", "player", "player", ("health", 100), ("armor", 3));
        var rogue = TestContent.AddActor(state, context.Content, "rogue", "hero_rogue", "player", "player", ("health", 80), ("armor", 1));

        var command = ai.ChooseCommand(goblin.RuntimeId);

        Assert.NotNull(command);
        Assert.Equal("basic_attack", command.ActionId);
        Assert.Equal(new[] { rogue.RuntimeId }, command.TargetActorIds);
        Assert.NotEqual(armored.RuntimeId, command.TargetActorIds[0]);
    }

    [Fact]
    public void ChooseCommand_TieDamage_PicksWeakerTarget()
    {
        var (state, ai, context) = Create();
        var goblin = TestContent.AddActor(state, context.Content, "goblin", "goblin", "enemy", "ai", ("attack", 7), ("health", 30));
        var tougher = TestContent.AddActor(state, context.Content, "hero", "hero_warrior", "player", "player", ("health", 50), ("armor", 0));
        var weaker = TestContent.AddActor(state, context.Content, "hero2", "hero_warrior", "player", "player", ("health", 30), ("armor", 0));

        var command = ai.ChooseCommand(goblin.RuntimeId);

        Assert.NotNull(command);
        Assert.Equal(new[] { weaker.RuntimeId }, command.TargetActorIds);
        Assert.NotEqual(tougher.RuntimeId, command.TargetActorIds[0]);
    }

    [Fact]
    public void ChooseCommand_ReturnsNull_ForDeadActor()
    {
        var (state, ai, context) = Create();
        var dead = TestContent.AddActor(state, context.Content, "goblin", "goblin", "enemy", "ai", ("health", 0));

        var command = ai.ChooseCommand(dead.RuntimeId);

        Assert.Null(command);
    }
}
