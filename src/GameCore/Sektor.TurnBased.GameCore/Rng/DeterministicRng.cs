namespace Sektor.TurnBased.GameCore.Rng;

/// <summary>
/// Детерминированный генератор случайных чисел на основе фиксированного Seed.
/// Гарантирует идентичные результаты на любых машинах при одинаковом начальном Seed.
/// </summary>
public sealed class DeterministicRng : IRngService
{
    private readonly Random _random;

    public DeterministicRng(int seed) => _random = new Random(seed);

    public int Next(int min, int max) => _random.Next(min, max);
    public double NextDouble() => _random.NextDouble();
}