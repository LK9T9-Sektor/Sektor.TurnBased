using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.Dialog.Commands;
using Sektor.TurnBased.Dialog.Content;
using Sektor.TurnBased.Dialog.Events;
using Sektor.TurnBased.Dialog.Model;

namespace Sektor.TurnBased.Dialog.Phases;

/// <summary>
/// Фаза ведения диалога: показывает узел и ждёт выбор игрока (Suspend) либо
/// запускает вложенный диалог через дочерний пайплайн (Resume и ожидание его
/// завершения). Узел без вариантов и без SubDialogId — концовка (Finish).
/// isSubDialog=true — режим вложенного диалога: концовка завершает только
/// дочерний пайплайн, исход родителя не трогается.
/// </summary>
public sealed class DialogFlowPhase : IGamePhase
{
    private readonly DialogState _state;
    private readonly DialogContent _content;
    private readonly DialogEventSink _sink;
    private readonly bool _isSubDialog;

    private GamePipeline? _parent;
    private GamePipeline? _subDialog;
    private string? _subDialogTriggerNodeId;

    public string Id => DialogPhaseIds.Flow;

    public DialogFlowPhase(DialogState state, DialogContent content, DialogEventSink sink, bool isSubDialog = false)
    {
        _state = state;
        _content = content;
        _sink = sink;
        _isSubDialog = isSubDialog;
    }

    public void Bind(GamePipeline pipeline) => _parent = pipeline;

    public Result<PhaseTransition> Execute(GameContext context)
    {
        if (_subDialog is not null)
        {
            if (!_subDialog.IsFinished)
                return Result<PhaseTransition>.Success(PhaseTransition.Resume());

            var trigger = GetNode(_subDialogTriggerNodeId!);
            if (trigger is null)
                return Result<PhaseTransition>.Failure($"Sub-dialog trigger node '{_subDialogTriggerNodeId}' not found.");

            _sink.SubDialogCompleted(trigger.SubDialogId!);
            _state.SetCurrentNode(trigger.ContinueNodeId!);
            _subDialog = null;
            return Result<PhaseTransition>.Success(PhaseTransition.Resume());
        }

        var nodeId = _state.CurrentNodeId;
        if (nodeId is null)
            return Result<PhaseTransition>.Failure("No current dialog node.");

        var node = GetNode(nodeId);
        if (node is null)
            return Result<PhaseTransition>.Failure($"Node '{nodeId}' not found in content.");

        if (!HasAllFlags(node.RequiresFlags))
            return Result<PhaseTransition>.Failure(
                $"Node '{nodeId}' requires unavailable flags: {string.Join(", ", node.RequiresFlags)}.");

        _state.AddFlags(node.GrantsFlags);

        if (node.Choices.Count == 0 && node.SubDialogId is null)
        {
            if (!_isSubDialog)
            {
                _state.SetOutcome(nodeId);
                _sink.DialogEnded(nodeId);
            }
            else
            {
                _sink.NodeShown(nodeId, node.Text, Array.Empty<string>());
            }
            return Result<PhaseTransition>.Success(PhaseTransition.Finish());
        }

        if (node.SubDialogId is not null)
            return StartSubDialog(context, node);

        _sink.NodeShown(nodeId, node.Text, node.Choices.Select(c => c.Id).ToList());
        return Result<PhaseTransition>.Success(PhaseTransition.Suspend("awaiting_choice"));
    }

    public Result<PhaseTransition?> OnCommand(GameContext context, IGameCommand command)
    {
        if (command is not ChooseOptionCommand choose)
            return Result<PhaseTransition?>.Success(null);

        var nodeId = _state.CurrentNodeId;
        if (nodeId is null)
            return Result<PhaseTransition?>.Failure("No current dialog node.");

        if (choose.NodeId != nodeId)
            return Result<PhaseTransition?>.Failure(
                $"Command is not for the current node. Expected '{nodeId}', got '{choose.NodeId}'.");

        var node = GetNode(nodeId);
        if (node is null)
            return Result<PhaseTransition?>.Failure($"Node '{nodeId}' not found in content.");

        if (node.Choices.Count == 0 || node.SubDialogId is not null)
            return Result<PhaseTransition?>.Failure($"Node '{nodeId}' cannot be answered.");

        var choice = node.Choices.FirstOrDefault(c => c.Id == choose.ChoiceId);
        if (choice is null)
            return Result<PhaseTransition?>.Failure($"Unknown choice '{choose.ChoiceId}' in node '{nodeId}'.");

        if (!HasAllFlags(choice.RequiresFlags))
            return Result<PhaseTransition?>.Failure(
                $"Choice '{nodeId}/{choice.Id}' requires unavailable flags: {string.Join(", ", choice.RequiresFlags)}.");

        _state.AddFlags(choice.GrantsFlags);
        _state.SetCurrentNode(choice.NextNodeId);
        _sink.ChoiceChosen(nodeId, choice.Id, choice.NextNodeId);
        return Result<PhaseTransition?>.Success(PhaseTransition.Resume());
    }

    private Result<PhaseTransition> StartSubDialog(GameContext context, DialogNodeDefinition node)
    {
        if (_parent is null)
            return Result<PhaseTransition>.Failure("Pipeline reference is missing (phase was not bound).");

        _sink.SubDialogEntered(node.SubDialogId!);
        _subDialogTriggerNodeId = node.Id;

        var child = _parent.CreateChildPipeline();
        var registerSetup = child.Register(new DialogSetupPhase(_state, _content, startOverride: node.SubDialogId));
        if (registerSetup.IsFailure)
            return Result<PhaseTransition>.Failure(registerSetup.Error!);

        var registerFlow = child.Register(new DialogFlowPhase(_state, _content, _sink, isSubDialog: true));
        if (registerFlow.IsFailure)
            return Result<PhaseTransition>.Failure(registerFlow.Error!);

        var start = child.Start(DialogPhaseIds.Setup);
        if (start.IsFailure)
            return Result<PhaseTransition>.Failure(start.Error!);

        _subDialog = child;
        return Result<PhaseTransition>.Success(PhaseTransition.Resume());
    }

    private bool HasAllFlags(IReadOnlyList<string> required) => required.All(_state.HasFlag);

    private DialogNodeDefinition? GetNode(string nodeId) =>
        _content.Nodes.FirstOrDefault(n => n.Id == nodeId);
}
