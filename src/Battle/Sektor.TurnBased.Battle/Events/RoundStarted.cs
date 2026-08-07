namespace Sektor.TurnBased.Battle.Events;

/// <summary>
/// Доменное событие: начался новый раунд (со снимком порядка ходов).
/// Поднимается через шину ядра.
/// </summary>
public sealed record RoundStarted(int RoundNumber, IReadOnlyList<string> Order);
