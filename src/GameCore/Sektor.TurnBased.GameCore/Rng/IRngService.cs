namespace Sektor.TurnBased.GameCore.Rng;

/// <summary>
/// Абстракция для генерации случайных чисел.
/// Позволяет заменять настоящий рандом на детерминированный для P2P-синхронизации и реплеев.
/// </summary>
public interface IRngService
{
    /// <summary>Возвращает случайное целое число в диапазоне [min, max].</summary>
    int Next(int min, int max);
    /// <summary>Возвращает случайное число с плавающей точкой от 0.0 до 1.0.</summary>
    double NextDouble();
}