namespace Sektor.TurnBased.Dialog.Model;

/// <summary>
/// Вариант ответа в узле диалога: текст кнопки, прекондиции по флагам,
/// флаги, выдаваемые при выборе, и следующий узел.
/// </summary>
public sealed record DialogChoiceDefinition(
    string Id,
    string Text,
    string NextNodeId,
    IReadOnlyList<string> RequiresFlags,
    IReadOnlyList<string> GrantsFlags);
