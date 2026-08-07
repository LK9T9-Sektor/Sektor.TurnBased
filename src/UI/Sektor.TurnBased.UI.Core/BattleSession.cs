using Sektor.TurnBased.Battle;
using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Battle.Rules;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// UI-адаптер боя: агрегирует BattleSnapshot из состояния движка, показывает
/// доступные действия текущего игрока и отображаемые имена юнитов.
/// </summary>
public sealed class BattleSession : GameSession
{
    private readonly BattleEngine _engine;

    public override string Kind { get; }

    private BattleSession(
        string kind,
        GameContext context,
        BattleEngine engine,
        IReadOnlyDictionary<string, string>? displayNames)
        : base(context, engine.Pipeline, displayNames)
    {
        Kind = kind;
        _engine = engine;
    }

    /// <summary>
    /// Создаёт бой: валидирует контент и регистрирует фазы (обёртка над BattleEngine.Create).
    /// Kind — разновидность боя (GameKinds.Battle / BattleLine); движок одинаков, отличается UI.
    /// </summary>
    public static Result<BattleSession> Create(
        GameContext context,
        ContentRegistry content,
        BattleContent battleContent,
        BattleConfig config,
        IReadOnlyDictionary<string, string>? displayNames = null,
        string kind = GameKinds.Battle)
    {
        var engineResult = BattleEngine.Create(context, content, battleContent, config);
        if (engineResult.IsFailure)
            return Result<BattleSession>.Failure(engineResult.Error!);

        return Result<BattleSession>.Success(new BattleSession(kind, context, engineResult.Value!, displayNames));
    }

    /// <summary>Снапшот состояния боя для отображения.</summary>
    public override BattleSnapshot Snapshot()
    {
        var state = _engine.State;
        var currentActorId = CurrentActorId(state);
        var actors = state.Actors.Select(ToUnit).ToList();

        return new BattleSnapshot(
            _engine.CurrentPhaseId ?? string.Empty,
            state.RoundNumber,
            state.TurnIndex,
            currentActorId,
            state.WinnerTeamId,
            TurnOrder(state),
            actors,
            AvailableActions(state, currentActorId));
    }

    protected override Result StartCore() => _engine.Start();

    private IReadOnlyList<string> TurnOrder(BattleState state) => state.Order.ToList();

    private string? CurrentActorId(BattleState state) =>
        state.Order.Count > 0 && state.TurnIndex < state.Order.Count
            ? state.Order[state.TurnIndex]
            : null;

    private IReadOnlyList<ActionOption> AvailableActions(BattleState state, string? currentActorId)
    {
        if (currentActorId is null)
            return Array.Empty<ActionOption>();

        var actor = state.GetActor(currentActorId);
        if (actor is null || !state.IsAlive(currentActorId) || actor.ControlledBy != "player")
            return Array.Empty<ActionOption>();

        if (!Context.Content.TryGet<ActorTemplateDefinition>(actor.TemplateId, out var template) || template is null)
            return Array.Empty<ActionOption>();

        var options = new List<ActionOption>();
        foreach (var actionId in template.ActionIds)
        {
            if (Context.Content.TryGet<ActionDefinition>(actionId, out var action) && action is not null)
                options.Add(new ActionOption(action.Id, action.Name, action.TargetMode));
        }

        return options;
    }

    private UnitSnapshot ToUnit(BattleActor actor)
    {
        var stats = new List<StatValueSnapshot>();
        StatValueSnapshot? vitalStat = null;

        foreach (var definition in _engine.State.Definitions.Values)
        {
            var current = actor.Resources.TryGetCurrent(definition.Id, out var currentValue) ? currentValue : 0;
            var max = actor.Resources.TryGetMax(definition.Id, out var maxValue) ? maxValue : (int?)null;
            var stat = new StatValueSnapshot(definition.Id, definition.Name, current, max);
            stats.Add(stat);
            if (definition.IsDeathStat)
                vitalStat = stat;
        }

        return new UnitSnapshot(
            actor.RuntimeId,
            DisplayNameFor(actor.TemplateId),
            actor.TeamId,
            DisplayNameFor(actor.TeamId),
            actor.TemplateId,
            actor.ControlledBy,
            _engine.State.IsAlive(actor.RuntimeId),
            stats,
            actor.Statuses.Select(s => s.StatusId).ToList(),
            vitalStat);
    }
}
