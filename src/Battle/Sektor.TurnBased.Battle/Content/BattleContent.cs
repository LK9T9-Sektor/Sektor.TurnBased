using Sektor.TurnBased.Battle.Effects;
using Sektor.TurnBased.Battle.Model;

namespace Sektor.TurnBased.Battle.Content;

/// <summary>
/// Типизированный набор боевого контента. Всё также зарегистрировано в ContentRegistry
/// по Id; здесь — для перечисления и валидации (реестр не умеет перечислять по типу).
/// </summary>
public sealed class BattleContent
{
    public IReadOnlyList<StatDefinition> Stats { get; }
    public IReadOnlyList<StatusDefinition> Statuses { get; }
    public IReadOnlyList<ActionDefinition> Actions { get; }
    public IReadOnlyList<ActorTemplateDefinition> Templates { get; }
    public IReadOnlyList<ICombatEffect> Effects { get; }
    public IReadOnlyList<ICombatPrecondition> Preconditions { get; }

    public BattleContent(
        IReadOnlyList<StatDefinition> stats,
        IReadOnlyList<StatusDefinition> statuses,
        IReadOnlyList<ActionDefinition> actions,
        IReadOnlyList<ActorTemplateDefinition> templates,
        IReadOnlyList<ICombatEffect> effects,
        IReadOnlyList<ICombatPrecondition> preconditions)
    {
        Stats = stats;
        Statuses = statuses;
        Actions = actions;
        Templates = templates;
        Effects = effects;
        Preconditions = preconditions;
    }
}
