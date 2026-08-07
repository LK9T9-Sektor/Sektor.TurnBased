using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Effects;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;
using Xunit;

namespace Sektor.TurnBased.Battle.Tests;

/// <summary>Тесты валидации контента на загрузке.</summary>
public class ContentValidatorTests
{
    [Fact]
    public void Validate_ValidCatalog_Succeeds()
    {
        var (content, battleContent) = TestContent.Build();

        var result = new ContentValidator().Validate(battleContent, content);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_UnknownPrecondition_Fails()
    {
        var (content, battleContent) = TestContent.Build();
        var broken = new ActionDefinition(
            "basic_attack",
            "Удар",
            BattleTargetModes.SingleEnemy,
            new[] { "missing_precondition" },
            new[] { "melee_damage" });
        var actions = battleContent.Actions.Where(a => a.Id != "basic_attack").Append(broken).ToList();
        var modified = new BattleContent(battleContent.Stats, battleContent.Statuses, actions, battleContent.Templates, battleContent.Effects, battleContent.Preconditions);

        var result = new ContentValidator().Validate(modified, content);

        Assert.True(result.IsFailure);
        Assert.Contains("missing_precondition", result.Error!);
    }

    [Fact]
    public void Validate_UnknownEffectStat_Fails()
    {
        var (content, battleContent) = TestContent.Build();
        var broken = new DamageEffect("bad_damage", "health", mitigationStatId: "nonexistent_stat");
        var effects = battleContent.Effects.Append(broken).ToList();
        var modified = new BattleContent(battleContent.Stats, battleContent.Statuses, battleContent.Actions, battleContent.Templates, effects, battleContent.Preconditions);

        var result = new ContentValidator().Validate(modified, content);

        Assert.True(result.IsFailure);
        Assert.Contains("nonexistent_stat", result.Error!);
    }

    [Fact]
    public void Validate_MissingDeathStat_Fails()
    {
        var (content, battleContent) = TestContent.Build();
        var stats = battleContent.Stats.Where(s => !s.IsDeathStat).ToList();
        var modified = new BattleContent(stats, battleContent.Statuses, battleContent.Actions, battleContent.Templates, battleContent.Effects, battleContent.Preconditions);

        var result = new ContentValidator().Validate(modified, content);

        Assert.True(result.IsFailure);
        Assert.Contains("death stat", result.Error!);
    }
}
