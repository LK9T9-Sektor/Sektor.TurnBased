namespace Sektor.TurnBased.Dialog.Events;

/// <summary>
/// Доменное событие: показан узел диалога с текстом и доступными вариантами.
/// Поднимается через шину ядра.
/// </summary>
public sealed record NodeShown(string NodeId, string Text, IReadOnlyList<string> ChoiceIds);
