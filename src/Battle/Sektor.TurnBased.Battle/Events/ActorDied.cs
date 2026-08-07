namespace Sektor.TurnBased.Battle.Events;

/// <summary>
/// Доменное событие: актор погиб (стат смерти достиг нуля). Поднимается через шину ядра.
/// </summary>
public sealed record ActorDied(string ActorRuntimeId, string? SourceActorId);
