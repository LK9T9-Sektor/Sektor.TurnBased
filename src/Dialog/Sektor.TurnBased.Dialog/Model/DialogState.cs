using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Dialog.Model;

/// <summary>
/// Состояние диалога: текущий узел, полученные флаги, посещённые узлы и исход.
/// Реализует маркер-контракт ядра IGameState. Мутируется только фазами.
/// </summary>
public sealed class DialogState : IGameState
{
    private readonly HashSet<string> _flags = new();
    private readonly List<string> _visited = new();

    public string? CurrentNodeId { get; private set; }

    public string? OutcomeNodeId { get; private set; }

    public IReadOnlyCollection<string> Flags => _flags;

    public IReadOnlyList<string> VisitedNodes => _visited;

    public bool HasFlag(string flagId) => _flags.Contains(flagId);

    public void AddFlags(IEnumerable<string> flags)
    {
        foreach (var flag in flags)
            _flags.Add(flag);
    }

    public void SetCurrentNode(string nodeId)
    {
        CurrentNodeId = nodeId;
        _visited.Add(nodeId);
    }

    public void SetOutcome(string outcomeNodeId) => OutcomeNodeId = outcomeNodeId;
}
