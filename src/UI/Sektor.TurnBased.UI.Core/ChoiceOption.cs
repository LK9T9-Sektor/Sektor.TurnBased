namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// Вариант ответа в узле диалога для отображения: идентификатор и текст кнопки.
/// </summary>
public sealed record ChoiceOption(
    string ChoiceId,
    string Text);
