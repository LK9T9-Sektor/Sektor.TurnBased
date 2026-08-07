using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.Dialog.Content;
using Sektor.TurnBased.Dialog.Model;

namespace Sektor.TurnBased.Dialog.Phases;

/// <summary>
/// Фаза настройки диалога: устанавливает стартовый узел. Для вложенных диалогов
/// стартовый узел передаётся через startOverride (дочерний пайплайн).
/// </summary>
public sealed class DialogSetupPhase : IGamePhase
{
    private readonly DialogState _state;
    private readonly DialogContent _content;
    private readonly string? _startOverride;

    public string Id => DialogPhaseIds.Setup;

    public DialogSetupPhase(DialogState state, DialogContent content, string? startOverride = null)
    {
        _state = state;
        _content = content;
        _startOverride = startOverride;
    }

    public Result<PhaseTransition> Execute(GameContext context)
    {
        var start = _startOverride ?? _content.StartNodeId;
        _state.SetCurrentNode(start);
        context.Log.Append($"Dialog started at {start}");
        return Result<PhaseTransition>.Success(PhaseTransition.Next(DialogPhaseIds.Flow));
    }
}
