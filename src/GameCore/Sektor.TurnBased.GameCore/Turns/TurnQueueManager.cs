using Sektor.TurnBased.GameCore.Actors;
using Sektor.TurnBased.GameCore.Rng;

namespace Sektor.TurnBased.GameCore.Turns;

/// <summary>
/// Генератор очереди ходов.
/// </summary>
public sealed class TurnQueueManager
{
    /// <summary>
    /// Генерирует список ID для BattleState.TurnOrder.
    /// </summary>
    public List<string> Generate<TTemplate>(
        IEnumerable<RuntimeActor<TTemplate>> actors,
        IRngService rng) where TTemplate : BaseActorTemplate
    {
        return actors
            .Where(a => !a.IsDead)
            .Select(a => new
            {
                Id = a.Id,
                Initiative = a.Template.BaseSpeed + rng.Next(1, 8)
            })
            .OrderByDescending(x => x.Initiative)
            .Select(x => x.Id)
            .ToList();
    }
}