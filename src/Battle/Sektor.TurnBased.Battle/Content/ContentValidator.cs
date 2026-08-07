using Sektor.TurnBased.Battle.Effects;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Content;

/// <summary>
/// Валидатор боевого контента на загрузке: все statId/effectId/preconditionId/statusId/
/// templateId обязаны существовать. Возвращает список всех ошибок (не только первой).
/// </summary>
public sealed class ContentValidator
{
    public Result Validate(BattleContent battleContent, ContentRegistry content)
    {
        if (battleContent is null)
            return Result.Failure("BattleContent cannot be null.");
        if (content is null)
            return Result.Failure("ContentRegistry cannot be null.");

        var failures = new List<string>();
        var statIds = battleContent.Stats.Select(s => s.Id).ToList();

        foreach (var group in battleContent.Stats.GroupBy(s => s.Id).Where(g => g.Count() > 1))
            failures.Add($"Duplicate stat '{group.Key}'.");

        var deathStats = battleContent.Stats.Count(s => s.IsDeathStat);
        if (deathStats != 1)
            failures.Add($"Exactly one death stat is required, found {deathStats}.");

        foreach (var action in battleContent.Actions)
        {
            if (!BattleTargetModes.All.Contains(action.TargetMode))
                failures.Add($"Action '{action.Id}' has unknown target mode '{action.TargetMode}'.");

            foreach (var preconditionId in action.Preconditions)
            {
                if (!content.TryGet<ICombatPrecondition>(preconditionId, out _))
                    failures.Add($"Action '{action.Id}' references unknown precondition '{preconditionId}'.");
            }

            foreach (var effectId in action.Effects)
            {
                if (!content.TryGet<ICombatEffect>(effectId, out _))
                    failures.Add($"Action '{action.Id}' references unknown effect '{effectId}'.");
            }
        }

        foreach (var status in battleContent.Statuses)
        {
            if (status.Duration < 1)
                failures.Add($"Status '{status.Id}' must have positive duration.");

            foreach (var statId in status.StatModifiers.Keys)
            {
                if (!statIds.Contains(statId))
                    failures.Add($"Status '{status.Id}' references unknown stat '{statId}'.");
            }

            if (status.TickEffectId is not null && !content.TryGet<ICombatEffect>(status.TickEffectId, out _))
                failures.Add($"Status '{status.Id}' references unknown tick effect '{status.TickEffectId}'.");
        }

        foreach (var template in battleContent.Templates)
        {
            if (template.ActionIds.Count == 0)
                failures.Add($"Template '{template.Id}' has no actions.");

            foreach (var statId in template.BaseStats.Keys)
            {
                if (!statIds.Contains(statId))
                    failures.Add($"Template '{template.Id}' references unknown stat '{statId}'.");
            }

            foreach (var actionId in template.ActionIds)
            {
                if (!content.TryGet<ActionDefinition>(actionId, out _))
                    failures.Add($"Template '{template.Id}' references unknown action '{actionId}'.");
            }
        }

        ValidateEffects(battleContent.Effects, statIds, content, failures);

        return failures.Count == 0
            ? Result.Success()
            : Result.Failure(string.Join("; ", failures));
    }

    private void ValidateEffects(
        IReadOnlyList<ICombatEffect> effects,
        List<string> statIds,
        ContentRegistry content,
        List<string> failures)
    {
        foreach (var effect in effects)
        {
            if (effect is DamageEffect damage)
            {
                CheckStat(damage.Id, damage.TargetStatId, statIds, failures);
                CheckOptionalStat(damage.Id, damage.SourceStatId, statIds, failures);
                CheckOptionalStat(damage.Id, damage.MitigationStatId, statIds, failures);
            }
            else if (effect is HealEffect heal)
            {
                CheckStat(heal.Id, heal.TargetStatId, statIds, failures);
                CheckOptionalStat(heal.Id, heal.SourceStatId, statIds, failures);
            }
            else if (effect is ModifyStatEffect modify)
            {
                CheckStat(modify.Id, modify.StatId, statIds, failures);
            }
            else if (effect is ApplyStatusEffect statusEffect)
            {
                if (!content.TryGet<StatusDefinition>(statusEffect.StatusId, out _))
                    failures.Add($"Effect '{statusEffect.Id}' references unknown status '{statusEffect.StatusId}'.");
            }
            else if (effect is SummonEffect summon)
            {
                if (!content.TryGet<ActorTemplateDefinition>(summon.TemplateId, out _))
                    failures.Add($"Effect '{summon.Id}' references unknown template '{summon.TemplateId}'.");
            }
        }
    }

    private static void CheckStat(string effectId, string statId, List<string> statIds, List<string> failures)
    {
        if (!statIds.Contains(statId))
            failures.Add($"Effect '{effectId}' references unknown stat '{statId}'.");
    }

    private static void CheckOptionalStat(string effectId, string? statId, List<string> statIds, List<string> failures)
    {
        if (statId is not null)
            CheckStat(effectId, statId, statIds, failures);
    }
}
