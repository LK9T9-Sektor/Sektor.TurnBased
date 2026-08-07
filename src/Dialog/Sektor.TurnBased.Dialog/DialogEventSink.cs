using Sektor.TurnBased.Core;
using Sektor.TurnBased.Dialog.Events;
using Sektor.TurnBased.Dialog.Model;

namespace Sektor.TurnBased.Dialog;

/// <summary>
/// Реализация IDialogEvents: поднимает события через GameEventBus ядра (для хук-логики)
/// с базовой логикой «визуализация + лог» (для UI). Визуальные события — immutable-снимки.
/// </summary>
public sealed class DialogEventSink : IDialogEvents
{
    private readonly GameContext _context;

    public DialogEventSink(GameContext context) => _context = context;

    public void NodeShown(string nodeId, string text, IReadOnlyList<string> choiceIds)
    {
        _context.Events.Raise(
            new NodeShown(nodeId, text, choiceIds),
            applyBase: e =>
            {
                _context.Visuals.Enqueue(new VisualEvent
                {
                    EventType = "NodeText",
                    SourceRuntimeId = nodeId,
                    TargetRuntimeId = nodeId,
                    Payload = e.ChoiceIds,
                });
                _context.Log.Append($"[{nodeId}] {text}");
            });
    }

    public void ChoiceChosen(string nodeId, string choiceId, string nextNodeId)
    {
        _context.Events.Raise(
            new ChoiceChosen(nodeId, choiceId, nextNodeId),
            applyBase: e =>
            {
                _context.Visuals.Enqueue(new VisualEvent
                {
                    EventType = "Choice",
                    SourceRuntimeId = nodeId,
                    TargetRuntimeId = nextNodeId,
                    Payload = choiceId,
                });
                _context.Log.Append($"{nodeId} -> {choiceId} -> {nextNodeId}");
            });
    }

    public void SubDialogEntered(string subDialogId)
    {
        _context.Events.Raise(
            new SubDialogEntered(subDialogId),
            applyBase: e =>
            {
                _context.Visuals.Enqueue(new VisualEvent
                {
                    EventType = "SubDialogEnter",
                    SourceRuntimeId = subDialogId,
                    TargetRuntimeId = subDialogId,
                });
                _context.Log.Append($"Sub-dialog '{subDialogId}' entered");
            });
    }

    public void SubDialogCompleted(string subDialogId)
    {
        _context.Events.Raise(
            new SubDialogCompleted(subDialogId),
            applyBase: e =>
            {
                _context.Visuals.Enqueue(new VisualEvent
                {
                    EventType = "SubDialogComplete",
                    SourceRuntimeId = subDialogId,
                    TargetRuntimeId = subDialogId,
                });
                _context.Log.Append($"Sub-dialog '{subDialogId}' completed");
            });
    }

    public void DialogEnded(string outcomeNodeId)
    {
        _context.Events.Raise(
            new DialogEnded(outcomeNodeId),
            applyBase: e =>
            {
                _context.Visuals.Enqueue(new VisualEvent
                {
                    EventType = "Ending",
                    SourceRuntimeId = outcomeNodeId,
                    TargetRuntimeId = outcomeNodeId,
                });
                _context.Log.Append($"Dialog ended at '{outcomeNodeId}'");
            });
    }
}
