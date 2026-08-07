namespace Sektor.TurnBased.Core;

/// <summary>
/// Детерминированный генератор случайных чисел на основе фиксированного зерна.
/// Одинаковый seed — одинаковые последовательности (важно для сети и реплеев).
/// </summary>
public sealed class DeterministicRng
{
    private readonly Random _random;

    public DeterministicRng(int seed) => _random = new Random(seed);

    /// <summary>Случайное целое число в диапазоне [min, max).</summary>
    public int Next(int min, int max)
    {
        if (max <= min)
            return min;
        return _random.Next(min, max);
    }

    /// <summary>Случайное число от 0.0 (вкл.) до 1.0 (искл.).</summary>
    public double NextDouble() => _random.NextDouble();
}
