using Sektor.TurnBased.Battle.Events;
using Sektor.TurnBased.Core;

namespace Sektor.TurnBased.Battle.Model;

/// <summary>
/// Контекст выполнения действия или прекондиции.
/// Содержит неизменяемые данные (источник, цели, RNG) и доступ к реестру и состоянию
/// (только чтение) + sink для эмиссии событий. Для AI/пре-коммита sink не задаётся.
/// </summary>
public sealed class ActionContext
{
    public string SourceActorId { get; }
    public IReadOnlyList<string> TargetActorIds { get; }
    public DeterministicRng Rng { get; }
    public ContentRegistry Content { get; }
    public BattleState State { get; }
    public ICombatEvents? Sink { get; }

    /// <summary>Шанс критического попадания (0 — критов нет).</summary>
    public double CritChance { get; }

    /// <summary>Множитель урона критического попадания.</summary>
    public double CritMultiplier { get; }

    public ActionContext(
        string sourceActorId,
        IReadOnlyList<string> targetActorIds,
        DeterministicRng rng,
        ContentRegistry content,
        BattleState state,
        ICombatEvents? sink = null,
        double critChance = 0,
        double critMultiplier = 1.5)
    {
        SourceActorId = sourceActorId;
        TargetActorIds = targetActorIds;
        Rng = rng;
        Content = content;
        State = state;
        Sink = sink;
        CritChance = critChance;
        CritMultiplier = critMultiplier;
    }

    public BattleActor? GetActor(string actorId) => State.GetActor(actorId);

    public bool IsAlive(string actorId) => State.IsAlive(actorId);

    public int EffectiveStat(string actorId, string statId) => State.EffectiveStat(actorId, statId);

    public int SourceEffectiveStat(string statId) => State.EffectiveStat(SourceActorId, statId);
}
