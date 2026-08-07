namespace Sektor.TurnBased.Dialog.Events;

/// <summary>
/// Доменное событие: игрок выбрал вариант ответа и диалог переходит к следующему узлу.
/// Поднимается через шину ядра.
/// </summary>
public sealed record ChoiceChosen(string NodeId, string ChoiceId, string NextNodeId);
