namespace Sektor.TurnBased.GameCore.Resolution;

/// <summary>
/// Детерминированный генератор на основе фиксированного Seed.
/// </summary>
public sealed class DeterministicRng(int seed) : IRngService
{
    private readonly Random _random = new(seed);
    public int Next(int min, int max) => _random.Next(min, max);
    public double NextDouble() => _random.NextDouble();
}