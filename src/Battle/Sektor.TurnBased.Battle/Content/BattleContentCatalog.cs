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
    /// <summary>Id героев, доступных игроку для выбора в мультиплеере.</summary>
    public static readonly string[] PlayerHeroIds = ["hero_warrior", "hero_rogue", "hero_archer", "hero_priestess"];

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
                new[] { "melee_damage" },
                Glyph: "⚔",
                Description: "Атака ближнего боя. Урон зависит от атаки и уменьшается бронёй цели."),
            new("power_attack", "Мощный удар", BattleTargetModes.SingleEnemy,
                new[] { "source_alive", "targets_alive" },
                new[] { "power_damage" },
                Glyph: "💥",
                Description: "Усиленная атака: +5 к урону по той же формуле, что и у обычного удара."),
            new("battle_rage", "Боевая ярость", BattleTargetModes.Self,
                new[] { "source_alive" },
                new[] { "rage_buff", "apply_rage" },
                Glyph: "🔥",
                Description: "Входит в ярость: +3 к атаке на 2 хода."),
            new("heal", "Лечение", BattleTargetModes.Self,
                new[] { "source_alive" },
                new[] { "heal_hp" },
                Glyph: "✚",
                Description: "Восстанавливает 20 здоровья."),
            new("strike_and_stun", "Оглушающий удар", BattleTargetModes.SingleEnemy,
                new[] { "source_alive", "targets_alive" },
                new[] { "melee_damage", "apply_stun" },
                Glyph: "⚡",
                Description: "Обычный удар плюс оглушение цели на 1 ход."),
        };

        var templates = new List<ActorTemplateDefinition>
        {
            // Герои (отряд из Blades)
            new("hero_warrior", "player", "player",
                new Dictionary<string, int> { ["health"] = 100, ["attack"] = 12, ["armor"] = 3, ["initiative"] = 8 },
                new[] { "basic_attack", "power_attack", "battle_rage", "heal" }),
            new("hero_rogue", "player", "player",
                new Dictionary<string, int> { ["health"] = 80, ["attack"] = 15, ["armor"] = 1, ["initiative"] = 12 },
                new[] { "basic_attack", "power_attack" }),
            new("hero_archer", "player", "player",
                new Dictionary<string, int> { ["health"] = 70, ["attack"] = 10, ["armor"] = 1, ["initiative"] = 10 },
                new[] { "basic_attack", "power_attack" }),
            new("hero_priestess", "player", "player",
                new Dictionary<string, int> { ["health"] = 90, ["attack"] = 5, ["armor"] = 2, ["initiative"] = 6 },
                new[] { "basic_attack", "heal" }),

            // Враги (нежить из Blades)
            new("skeleton", "enemy", "ai",
                new Dictionary<string, int> { ["health"] = 30, ["attack"] = 7, ["armor"] = 0, ["initiative"] = 5 },
                new[] { "basic_attack" }),
            new("zombie", "enemy", "ai",
                new Dictionary<string, int> { ["health"] = 60, ["attack"] = 4, ["armor"] = 2, ["initiative"] = 3 },
                new[] { "basic_attack" }),
            new("skeleton_archer", "enemy", "ai",
                new Dictionary<string, int> { ["health"] = 25, ["attack"] = 9, ["armor"] = 0, ["initiative"] = 7 },
                new[] { "basic_attack", "power_attack" }),
            new("skeleton_mage", "enemy", "ai",
                new Dictionary<string, int> { ["health"] = 20, ["attack"] = 6, ["armor"] = 0, ["initiative"] = 6 },
                new[] { "basic_attack", "heal" }),
        };

        foreach (string heroId in PlayerHeroIds)
        {
            if (!templates.Any(t => t.Id == heroId))
                failures.Add($"Hero '{heroId}' from PlayerHeroIds is not registered.");
        }

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
