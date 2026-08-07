using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Model;

/// <summary>
/// Контейнер текущих и максимальных значений статов актора.
/// Чистый сервис: не зависит от шины и очереди — изменения возвращает как StatChange.
/// Никогда не бросает исключений: ошибки через Result.
/// </summary>
public sealed class ResourceContainer
{
    private readonly IReadOnlyDictionary<string, StatDefinition> _definitions;
    private readonly Dictionary<string, int> _current = new();
    private readonly Dictionary<string, int> _max = new();

    public ResourceContainer(IReadOnlyDictionary<string, StatDefinition> definitions)
    {
        _definitions = definitions;
    }

    /// <summary>Количество известных статов.</summary>
    public int Count => _current.Count;

    /// <summary>Задаёт начальное значение стата (текущее и максимальное).</summary>
    public Result SetInitial(string statId, int value)
    {
        if (!_definitions.ContainsKey(statId))
            return Result.Failure($"Stat '{statId}' is not defined.");

        _current[statId] = value;
        _max[statId] = value;
        return Result.Success();
    }

    public bool TryGetCurrent(string statId, out int value) => _current.TryGetValue(statId, out value);

    public bool TryGetMax(string statId, out int value) => _max.TryGetValue(statId, out value);

    /// <summary>
    /// Применяет дельту к стату с клампом по определению и возвращает снимок изменения.
    /// null — стат неизвестен или значение не изменилось. Не кламптит на максимальное
    /// значение: верхний предел применяют эффекты (HealEffect), когда это семантически нужно.
    /// </summary>
    public StatChange? ModifyStat(string statId, int delta)
    {
        if (!_current.TryGetValue(statId, out var current) || !_definitions.TryGetValue(statId, out var definition))
            return null;

        var next = current + delta;

        if (definition.Max is not null)
            next = Math.Min(next, definition.Max.Value);

        if (definition.ClampMin)
            next = Math.Max(next, definition.Min ?? 0);

        if (next == current)
            return null;

        _current[statId] = next;
        return new StatChange(statId, next - current, next);
    }

    /// <summary>
    /// Семантическое лечение: увеличивает значение, но не выше максимального.
    /// Специально вынесено отдельно, чтобы не завязывать верхний кламп в ModifyStat.
    /// </summary>
    public StatChange? Heal(string statId, int amount)
    {
        if (amount <= 0 || !_current.TryGetValue(statId, out var current))
            return null;

        var max = _max.TryGetValue(statId, out var m) ? m : current + amount;
        var next = Math.Min(current + amount, max);
        if (next == current)
            return null;

        _current[statId] = next;
        return new StatChange(statId, next - current, next);
    }
}
