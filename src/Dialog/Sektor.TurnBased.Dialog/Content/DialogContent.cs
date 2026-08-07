using Sektor.TurnBased.Dialog.Model;

namespace Sektor.TurnBased.Dialog.Content;

/// <summary>
/// Типизированный набор контента диалога. Всё также зарегистрировано в ContentRegistry
/// по Id; здесь — для перечисления и валидации (реестр не умеет перечислять по типу).
/// </summary>
public sealed class DialogContent
{
    public IReadOnlyList<DialogNodeDefinition> Nodes { get; }

    public string StartNodeId { get; }

    /// <summary>Все объявленные флаги квеста (для валидации ссылок на загрузке).</summary>
    public IReadOnlyList<string> DeclaredFlags { get; }

    public DialogContent(
        IReadOnlyList<DialogNodeDefinition> nodes,
        string startNodeId,
        IReadOnlyList<string> declaredFlags)
    {
        Nodes = nodes;
        StartNodeId = startNodeId;
        DeclaredFlags = declaredFlags;
    }
}
