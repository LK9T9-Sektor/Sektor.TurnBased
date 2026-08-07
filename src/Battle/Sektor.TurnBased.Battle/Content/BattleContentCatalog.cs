using Sektor.TurnBased.Battle.Effects;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Content;

/// <summary>
/// Sektor-подобный демо-контент: статы, статусы, эффекты, прекондиции, действия и шаблоны.
/// Регистрирует всё в ContentRegistry по Id и возвращает типизированный BattleContent.
/// </summary>
public static class BattleContentCatalog
{
    public static Result<BattleContent> Build(ContentRegistry content)
    {
        var failures = new List<string>();

        var stats = new List<StatDefinition>
        {
            new("health", "Здоровье", Min: 0, ClampMin: true, IsDeathStat: true),
            new("attack", "Атака"),
            new("armor", "Броня"),
            new("initiative", "Инициатива"),
        };

        var statuses = new List<StatusDefinition>
        {
            new("rage", new Dictionary<string, int> { ["attack"] = 3 }, Duration: 2),
            new("stunned", new Dictionary<string, int>(), Duration: 1, BlocksTurn: true),
        };

        var effects = new List<ICombatEffect>
        {
            new DamageEffect("melee_damage", "health", sourceStatId: "attack", mitigationStatId: "armor"),
            new DamageEffect("power_damage", "health", amount: 5, sourceStatId: "attack", mitigationStatId: "armor"),
            new HealEffect("heal_hp", "health", amount: 20),
            new ModifyStatEffect("rage_buff", "attack", 3),
            new ApplyStatusEffect("apply_rage", "rage"),
            new ApplyStatusEffect("apply_stun", "stunned"),
        };

        var preconditions = new List<ICombatPrecondition>
        {
            new SourceAlivePrecondition("source_alive"),
            new TargetsAlivePrecondition("targets_alive"),
        };

        var actions = new List<ActionDefinition>
        {
            new("basic_attack", "Удар", BattleTargetModes.SingleEnemy,
                new[] { "source_alive", "targets_alive" },
                new[] { "melee_damage" }),
            new("power_attack", "Мощный удар", BattleTargetModes.SingleEnemy,
                new[] { "source_alive", "targets_alive" },
                new[] { "power_damage" }),
            new("battle_rage", "Боевая ярость", BattleTargetModes.Self,
                new[] { "source_alive" },
                new[] { "rage_buff", "apply_rage" }),
            new("heal", "Лечение", BattleTargetModes.Self,
                new[] { "source_alive" },
                new[] { "heal_hp" }),
            new("strike_and_stun", "Оглушающий удар", BattleTargetModes.SingleEnemy,
                new[] { "source_alive", "targets_alive" },
                new[] { "melee_damage", "apply_stun" }),
        };

        var templates = new List<ActorTemplateDefinition>
        {
            new("hero_warrior", "player", "player",
                new Dictionary<string, int> { ["health"] = 100, ["attack"] = 12, ["armor"] = 3, ["initiative"] = 8 },
                new[] { "basic_attack", "power_attack", "battle_rage", "heal" }),
            new("hero_rogue", "player", "player",
                new Dictionary<string, int> { ["health"] = 80, ["attack"] = 15, ["armor"] = 1, ["initiative"] = 12 },
                new[] { "basic_attack", "power_attack" }),
            new("goblin", "enemy", "ai",
                new Dictionary<string, int> { ["health"] = 30, ["attack"] = 7, ["armor"] = 0, ["initiative"] = 5 },
                new[] { "basic_attack" }),
            new("ogre", "enemy", "ai",
                new Dictionary<string, int> { ["health"] = 70, ["attack"] = 10, ["armor"] = 4, ["initiative"] = 3 },
                new[] { "basic_attack", "power_attack", "strike_and_stun" }),
            new("goblin_shaman", "enemy", "ai",
                new Dictionary<string, int> { ["health"] = 25, ["attack"] = 5, ["armor"] = 0, ["initiative"] = 6 },
                new[] { "basic_attack", "heal" }),
        };

        foreach (var stat in stats)
            Add(content, stat.Id, stat, failures);
        foreach (var status in statuses)
            Add(content, status.Id, status, failures);
        foreach (var action in actions)
            Add(content, action.Id, action, failures);
        foreach (var template in templates)
            Add(content, template.Id, template, failures);
        foreach (var effect in effects)
            Add(content, effect.Id, effect, failures);
        foreach (var precondition in preconditions)
            Add(content, precondition.Id, precondition, failures);

        if (failures.Count > 0)
            return Result<BattleContent>.Failure(string.Join("; ", failures));

        return Result<BattleContent>.Success(
            new BattleContent(stats, statuses, actions, templates, effects, preconditions));
    }

    private static void Add(ContentRegistry content, string id, object item, List<string> failures)
    {
        var result = content.Register(id, item);
        if (result.IsFailure)
            failures.Add(result.Error!);
    }
}
