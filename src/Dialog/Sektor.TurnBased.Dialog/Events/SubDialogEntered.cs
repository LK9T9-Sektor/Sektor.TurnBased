namespace Sektor.TurnBased.Dialog.Events;

/// <summary>
/// Доменное событие: запущен вложенный диалог через дочерний пайплайн ядра.
/// Поднимается через шину ядра.
/// </summary>
public sealed record SubDialogEntered(string SubDialogId);
