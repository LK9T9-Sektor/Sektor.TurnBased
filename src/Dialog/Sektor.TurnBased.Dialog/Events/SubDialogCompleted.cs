namespace Sektor.TurnBased.Dialog.Events;

/// <summary>
/// Доменное событие: вложенный диалог завершён, родительский диалог продолжается.
/// Поднимается через шину ядра.
/// </summary>
public sealed record SubDialogCompleted(string SubDialogId);
