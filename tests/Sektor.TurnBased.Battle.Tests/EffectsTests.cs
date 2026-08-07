using Sektor.TurnBased.Battle.Effects;
using Sektor.TurnBased.Core.Abstractions;
using Xunit;

namespace Sektor.TurnBased.Battle.Tests;

/// <summary>Тесты эффектов: урон с/без митигации, лечение, статусы, прекондиции.</summary>
public class EffectsTests
{
    [Fact]
    public void DamageEffect_WithMitigation_ReducesDamage()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var source = TestContent.AddActor(state, content, "hero", "hero_warrior", "player", "player", ("attack", 10));
        var target = TestContent.AddActor(state, content, "goblin", "goblin", "enemy", "ai", ("health", 30), ("armor", 4));
        var context = TestContent.CreateContext(state, content, source.RuntimeId, new[] { target.RuntimeId });

        var effect = new DamageEffect("dmg", "health", sourceStatId: "attack", mitigationStatId: "armor");
        var result = effect.Apply(context);

        Assert.True(result.IsSuccess);
        Assert.True(target.Resources.TryGetCurrent("health", out var health));
        Assert.Equal(24, health);
    }

    [Fact]
    public void DamageEffect_WithoutMitigation_AppliesFullDamage()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var source = TestContent.AddActor(state, content, "hero", "hero_warrior", "player", "player", ("attack", 10));
        var target = TestContent.AddActor(state, content, "goblin", "goblin", "enemy", "ai", ("health", 30));
        var context = TestContent.CreateContext(state, content, source.RuntimeId, new[] { target.RuntimeId });

        var effect = new DamageEffect("dmg", "health", sourceStatId: "attack");
        var result = effect.Apply(context);

        Assert.True(result.IsSuccess);
        Assert.True(target.Resources.TryGetCurrent("health", out var health));
        Assert.Equal(20, health);
    }

    [Fact]
    public void DamageEffect_EstimateDamage_MatchesAppliedDamage()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var source = TestContent.AddActor(state, content, "hero", "hero_warrior", "player", "player", ("attack", 10));
        var target = TestContent.AddActor(state, content, "goblin", "goblin", "enemy", "ai", ("health", 30), ("armor", 4));
        var context = TestContent.CreateContext(state, content, source.RuntimeId, new[] { target.RuntimeId });

        var effect = new DamageEffect("dmg", "health", sourceStatId: "attack", mitigationStatId: "armor");

        Assert.Equal(6, effect.EstimateDamage(context, target.RuntimeId));
    }

    [Fact]
    public void HealEffect_ClampsToMax()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var source = TestContent.AddActor(state, content, "hero", "hero_warrior", "player", "player", ("health", 30));
        source.Resources.ModifyStat("health", -10);
        var context = TestContent.CreateContext(state, content, source.RuntimeId, new[] { source.RuntimeId });

        var effect = new HealEffect("heal", "health", amount: 100);
        var result = effect.Apply(context);

        Assert.True(result.IsSuccess);
        Assert.True(source.Resources.TryGetCurrent("health", out var health));
        Assert.Equal(30, health);
    }

    [Fact]
    public void ApplyStatusEffect_AddsStatusWithModifiers()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var source = TestContent.AddActor(state, content, "hero", "hero_warrior", "player", "player", ("attack", 5));
        var context = TestContent.CreateContext(state, content, source.RuntimeId, new[] { source.RuntimeId });

        var effect = new ApplyStatusEffect("rage", "rage");
        var result = effect.Apply(context);

        Assert.True(result.IsSuccess);
        Assert.Single(source.Statuses);
        Assert.Equal(8, state.EffectiveStat(source.RuntimeId, "attack"));
    }

    [Fact]
    public void ModifyStatEffect_AppliesRawDelta()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var source = TestContent.AddActor(state, content, "hero", "hero_warrior", "player", "player", ("attack", 5));
        var context = TestContent.CreateContext(state, content, source.RuntimeId, new[] { source.RuntimeId });

        var effect = new ModifyStatEffect("buff", "attack", 3);
        var result = effect.Apply(context);

        Assert.True(result.IsSuccess);
        Assert.True(source.Resources.TryGetCurrent("attack", out var attack));
        Assert.Equal(8, attack);
    }

    [Fact]
    public void TargetsAlivePrecondition_FailsForDeadTarget()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var source = TestContent.AddActor(state, content, "hero", "hero_warrior", "player", "player", ("health", 10));
        var dead = TestContent.AddActor(state, content, "goblin", "goblin", "enemy", "ai", ("health", 0));
        var context = TestContent.CreateContext(state, content, source.RuntimeId, new[] { dead.RuntimeId });

        var precondition = new TargetsAlivePrecondition("targets_alive");
        var result = precondition.CanApply(context);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public void HasResourcePrecondition_FailsWhenStatTooLow()
    {
        var (content, battleContent) = TestContent.Build();
        var state = TestContent.CreateState(battleContent);
        var source = TestContent.AddActor(state, content, "hero", "hero_warrior", "player", "player", ("health", 10));
        var context = TestContent.CreateContext(state, content, source.RuntimeId, new[] { source.RuntimeId });

        var precondition = new HasResourcePrecondition("has_hp", "health", 50);
        var result = precondition.CanApply(context);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }
}
