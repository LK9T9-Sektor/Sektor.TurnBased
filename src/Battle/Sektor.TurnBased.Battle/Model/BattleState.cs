using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Model;

/// <summary>
/// Состояние боя: акторы, определения статов, раунд, порядок ходов и победитель.
/// Реализует маркер-контракт ядра IGameState. Мутируется только фазами и исполнителем.
/// </summary>
public sealed class BattleState : IGameState
{
    private readonly List<BattleActor> _actors = new();
    private int _actorIdCounter;

    public IReadOnlyList<BattleActor> Actors => _actors;
    public IReadOnlyDictionary<string, StatDefinition> Definitions { get; }
    public int RoundNumber { get; set; }
    public int TurnIndex { get; set; }
    public IReadOnlyList<string> Order { get; set; } = new List<string>();
    public string? WinnerTeamId { get; set; }
    public bool IsFinished { get; set; }

    public BattleState(IReadOnlyDictionary<string, StatDefinition> definitions)
    {
        Definitions = definitions;
    }

    /// <summary>Id стата смерти (первый с IsDeathStat) или null, если не задан.</summary>
    public string? DeathStatId => Definitions.Values.FirstOrDefault(d => d.IsDeathStat)?.Id;

    /// <summary>Генерирует уникальный runtime-Id актора (детерминированно).</summary>
    public string NewActorId(string hint) => $"{hint}_{_actorIdCounter++}";

    public void AddActor(BattleActor actor) => _actors.Add(actor);

    public BattleActor? GetActor(string runtimeId) => _actors.FirstOrDefault(a => a.RuntimeId == runtimeId);

    public bool IsAlive(string runtimeId)
    {
        var actor = GetActor(runtimeId);
        if (actor is null)
            return false;
        if (DeathStatId is null)
            return true;
        return actor.Resources.TryGetCurrent(DeathStatId, out var hp) && hp > 0;
    }

    public IEnumerable<BattleActor> AliveActors() => _actors.Where(a => IsAlive(a.RuntimeId));

    public IEnumerable<BattleActor> AliveActorsOfTeam(string teamId) =>
        _actors.Where(a => a.TeamId == teamId && IsAlive(a.RuntimeId));

    /// <summary>
    /// Эффективное значение стата: база (текущее значение) плюс модификаторы статусов.
    /// Неизвестный актор или стат — 0.
    /// </summary>
    public int EffectiveStat(string runtimeId, string statId)
    {
        var actor = GetActor(runtimeId);
        if (actor is null || !actor.Resources.TryGetCurrent(statId, out var baseValue))
            return 0;

        var total = baseValue;
        foreach (var status in actor.Statuses)
        {
            if (status.StatModifiers.TryGetValue(statId, out var delta))
                total += delta;
        }
        return total;
    }
}
