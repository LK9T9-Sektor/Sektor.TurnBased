using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.Dialog;
using Sektor.TurnBased.Dialog.Content;

namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// UI-адаптер диалога: агрегирует DialogSnapshot из состояния движка и контента,
/// отдаёт текущий узел, текст и варианты ответа.
/// </summary>
public sealed class DialogSession : GameSession
{
    private readonly DialogEngine _engine;
    private readonly DialogContent _content;

    public override string Kind => GameKinds.Dialog;

    private DialogSession(
        GameContext context,
        DialogEngine engine,
        DialogContent content,
        IReadOnlyDictionary<string, string>? displayNames)
        : base(context, engine.Pipeline, displayNames)
    {
        _engine = engine;
        _content = content;
    }

    /// <summary>
    /// Создаёт диалог: валидирует контент и регистрирует фазы (обёртка над DialogEngine.Create).
    /// </summary>
    public static Result<DialogSession> Create(
        GameContext context,
        ContentRegistry content,
        DialogContent dialogContent,
        IReadOnlyDictionary<string, string>? displayNames = null)
    {
        var engineResult = DialogEngine.Create(context, content, dialogContent);
        if (engineResult.IsFailure)
            return Result<DialogSession>.Failure(engineResult.Error!);

        return Result<DialogSession>.Success(
            new DialogSession(context, engineResult.Value!, dialogContent, displayNames));
    }

    /// <summary>Снапшот состояния диалога для отображения.</summary>
    public override DialogSnapshot Snapshot()
    {
        var node = _content.Nodes.FirstOrDefault(n => n.Id == _engine.State.CurrentNodeId);
        var choices = node?.Choices.Select(c => new ChoiceOption(c.Id, c.Text)).ToList()
            ?? new List<ChoiceOption>();

        return new DialogSnapshot(
            _engine.CurrentPhaseId ?? string.Empty,
            node?.Id,
            node?.Text,
            choices,
            _engine.Outcome);
    }

    protected override Result StartCore() => _engine.Start();
}
