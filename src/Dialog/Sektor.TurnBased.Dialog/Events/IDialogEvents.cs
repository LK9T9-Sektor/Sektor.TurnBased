namespace Sektor.TurnBased.Dialog.Events;

/// <summary>
/// Контракт событий диалога. Реализация поднимает события через GameEventBus ядра
/// (для хук-логики) с базовой логикой «визуализация + лог» (для UI).
/// </summary>
public interface IDialogEvents
{
    void NodeShown(string nodeId, string text, IReadOnlyList<string> choiceIds);

    void ChoiceChosen(string nodeId, string choiceId, string nextNodeId);

    void SubDialogEntered(string subDialogId);

    void SubDialogCompleted(string subDialogId);

    void DialogEnded(string outcomeNodeId);
}
