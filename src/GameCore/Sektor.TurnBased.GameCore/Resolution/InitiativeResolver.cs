using Sektor.TurnBased.GameCore.Actors;
using Sektor.TurnBased.GameCore.Rng;

namespace Sektor.TurnBased.GameCore.Resolution;

/// <summary>
/// Вычисляет очередность ходов на основе характеристик актёров.
/// Реализует стратегию расчёта инициативы.
/// </summary>
public sealed class InitiativeResolver
{
    /// <summary>
    /// Генерирует список ID актёров в порядке их хода.
    /// Формула: BaseSpeed + случайный бонус (1..8).
    /// </summary>
    public List<string> Resolve<TTemplate>(
        IEnumerable<BattleActor<TTemplate>> actors,
        IRngService rng) where TTemplate : BaseActorTemplate
    {
        return actors
            .Where(a => !a.IsDead)
            .Select(a => new
            {
                Id = a.Id,
                Initiative = a.Template.BaseSpeed + rng.Next(1, 9)
            })
            .OrderByDescending(x => x.Initiative)
            .Select(x => x.Id)
            .ToList();
    }
}