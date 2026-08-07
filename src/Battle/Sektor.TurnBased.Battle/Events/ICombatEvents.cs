using Sektor.TurnBased.Battle.Model;

namespace Sektor.TurnBased.Battle.Events;

/// <summary>
/// Эмиттер боевых событий из эффектов и фаз (sink).
/// В бою реализуется BattleEventSink: поднимает события через шину ядра и пишет
/// визуал/лог. В пре-коммит контексте (AI) не задаётся.
/// </summary>
public interface ICombatEvents
{
    void StatChanged(string actorId, StatChange change, string? sourceActorId);

    void ActorDied(string actorId, string? sourceActorId);

    void StatusApplied(string actorId, BattleStatus status);

    /// <summary>Добавляет призванного актора в состояние и сообщает о нём.</summary>
    void ActorSummoned(BattleActor actor);

    void TurnBlocked(string actorId);
}
