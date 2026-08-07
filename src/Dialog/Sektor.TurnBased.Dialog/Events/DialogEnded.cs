namespace Sektor.TurnBased.Dialog.Events;

/// <summary>
/// Доменное событие: диалог завершился на узле-концовке (Outcome).
/// Поднимается через шину ядра.
/// </summary>
public sealed record DialogEnded(string OutcomeNodeId);
