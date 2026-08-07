namespace Sektor.TurnBased.Battle.Events;

/// <summary>
/// Доменное событие: бой завершён. WinnerTeamId — null при ничьей.
/// Поднимается через шину ядра.
/// </summary>
public sealed record BattleEnded(string? WinnerTeamId);
