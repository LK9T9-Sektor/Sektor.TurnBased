using Sektor.TurnBased.GameCore.Entities;

namespace Sektor.TurnBased.GameCore.Resolution;

/// <summary>
/// Вычисляет очередь ходов на основе атрибутов актёров.
/// Стратегия сортировки вынесена сюда, чтобы ядро не зависело от конкретной формулы.
/// </summary>
public sealed class InitiativeResolver
{
    public List<string> Resolve(IEnumerable<BattleActor> actors, IRngService rng)
    {
        return actors
            .Where(a => !a.IsDead)
            .Select(a => new
            {
                Id = a.Id,
                Initiative = a.GetAttribute<int>("Speed") + rng.Next(1, 9)
            })
            .OrderByDescending(x => x.Initiative)
            .Select(x => x.Id)
            .ToList();
    }
}