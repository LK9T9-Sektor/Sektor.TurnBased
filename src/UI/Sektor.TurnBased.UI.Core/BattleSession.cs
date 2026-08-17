using Sektor.TurnBased.Battle;
using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Battle.Rules;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// UI-адаптер боя: агрегирует BattleSnapshot из состояния движка, показывает
/// доступные действия текущего игрока и отображаемые имена юнитов. В мультиплеере
/// (localSlot задан) действия доступны только юнитам локального слота.
/// </summary>
public class BattleSession : GameSession
{
    private readonly BattleEngine _engine;
    private readonly IReadOnlyList<PlayerPresentation>? _presentations;
    private readonly int? _localSlot;

    public override string Kind { get; }

    protected BattleSession(
        string kind,
        GameContext context,
        BattleEngine engine,
        IReadOnlyDictionary<string, string>? displayNames,
        IReadOnlyList<PlayerPresentation>? presentations = null,
        int? localSlot = null)
        : base(context, engine.Pipeline, displayNames)
    {
        Kind = kind;
        _engine = engine;
        _presentations = presentations;
        _localSlot = localSlot;
    }

    /// <summary>
    /// Создаёт бой: валидирует контент и регистрирует фазы (обёртка над BattleEngine.Create).
    /// Kind — разновидность боя (GameKinds.Battle / BattleLine); движок одинаков, отличается UI.
    /// spawns — ростер состава боя; presentations/localSlot — мультиплеерные атрибуты.
    /// </summary>
    public static Result<BattleSession> Create(
        GameContext context,
        ContentRegistry content,
        BattleContent battleContent,
        BattleConfig config,
        IReadOnlyDictionary<string, string>? displayNames = null,
        string kind = GameKinds.Battle,
        IReadOnlyList<BattleSpawn>? spawns = null,
        IReadOnlyList<PlayerPresentation>? presentations = null,
        int? localSlot = null)
    {
        var engineResult = BuildEngine(context, content, battleContent, config, spawns);
        if (engineResult.IsFailure)
            return Result<BattleSession>.Failure(engineResult.Error!);

        return Result<BattleSession>.Success(
            new BattleSession(kind, context, engineResult.Value!, displayNames, presentations, localSlot));
    }

    /// <summary>Строит движок боя (общая точка для одиночной и сетевой сессий).</summary>
    protected static Result<BattleEngine> BuildEngine(
        GameContext context,
        ContentRegistry content,
        BattleContent battleContent,
        BattleConfig config,
        IReadOnlyList<BattleSpawn>? spawns = null)
    {
        return BattleEngine.Create(context, content, battleContent, config, spawns: spawns);
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
            AvailableActions(state, currentActorId),
            IsLocalTurn(state, currentActorId));
    }

    protected override Result StartCore() => _engine.Start();

    private IReadOnlyList<string> TurnOrder(BattleState state) => state.Order.ToList();

    private string? CurrentActorId(BattleState state) =>
        state.Order.Count > 0 && state.TurnIndex < state.Order.Count
            ? state.Order[state.TurnIndex]
            : null;

    /// <summary>
    /// Ход локального игрока: актор управляется человеком и (в мультиплеере)
    /// принадлежит локальному слоту. Одиночная игра — любой человеческий актор.
    /// </summary>
    private bool IsLocalTurn(BattleState state, string? currentActorId)
    {
        if (currentActorId is null)
            return false;

        var actor = state.GetActor(currentActorId);
        if (actor is null || !actor.IsHumanControlled)
            return false;

        return _localSlot is null || IsSlot(actor.ControlledBy, _localSlot.Value);
    }

    private static bool IsSlot(string controlledBy, int slot) =>
        controlledBy.StartsWith("player_", StringComparison.Ordinal)
        && int.TryParse(controlledBy.AsSpan("player_".Length), out var parsed)
        && parsed == slot;

    private IReadOnlyList<ActionOption> AvailableActions(BattleState state, string? currentActorId)
    {
        if (currentActorId is null)
            return Array.Empty<ActionOption>();

        var actor = state.GetActor(currentActorId);
        if (actor is null || !state.IsAlive(currentActorId) || !actor.IsHumanControlled)
            return Array.Empty<ActionOption>();

        if (_localSlot is { } slot && !IsSlot(actor.ControlledBy, slot))
            return Array.Empty<ActionOption>();

        if (!Context.Content.TryGet<ActorTemplateDefinition>(actor.TemplateId, out var template) || template is null)
            return Array.Empty<ActionOption>();

        var options = new List<ActionOption>();
        foreach (var actionId in template.ActionIds)
        {
            if (Context.Content.TryGet<ActionDefinition>(actionId, out var action) && action is not null)
                options.Add(new ActionOption(action.Id, action.Name, action.TargetMode, action.Glyph, action.Description));
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

        string? playerName = null;
        string? playerColor = null;
        if (_presentations is not null && IsSlot(actor.ControlledBy, out var slot) && slot < _presentations.Count)
        {
            playerName = _presentations[slot].Name;
            playerColor = _presentations[slot].ColorHex;
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
            vitalStat,
            playerName,
            playerColor);
    }

    private static bool IsSlot(string controlledBy, out int slot)
    {
        slot = -1;
        if (!controlledBy.StartsWith("player_", StringComparison.Ordinal))
            return false;
        return int.TryParse(controlledBy.AsSpan("player_".Length), out slot);
    }
}
