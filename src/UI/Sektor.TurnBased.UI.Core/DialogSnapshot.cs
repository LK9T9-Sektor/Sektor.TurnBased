namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// Снимок состояния диалога для отображения: текущий узел, текст, варианты и
/// узел-исход (если диалог завершён). Не содержит ссылок на движок.
/// </summary>
public sealed record DialogSnapshot(
    string PhaseId,
    string? NodeId,
    string? NodeText,
    IReadOnlyList<ChoiceOption> Choices,
    string? OutcomeNodeId);
