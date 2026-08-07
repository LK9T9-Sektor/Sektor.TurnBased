namespace Sektor.TurnBased.Dialog.Model;

/// <summary>
/// Определение узла диалога (данные-ресурс). Узел — текст плюс варианты ответа,
/// либо триггер вложенного диалога (SubDialogId). Узел без вариантов и без
/// SubDialogId — концовка (Outcome).
/// </summary>
public sealed record DialogNodeDefinition(
    string Id,
    string Text,
    IReadOnlyList<DialogChoiceDefinition> Choices,
    IReadOnlyList<string> RequiresFlags,
    IReadOnlyList<string> GrantsFlags,
    string? SubDialogId = null,
    string? ContinueNodeId = null);
