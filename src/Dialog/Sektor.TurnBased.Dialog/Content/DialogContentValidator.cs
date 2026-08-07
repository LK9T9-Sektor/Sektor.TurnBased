using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.Dialog.Model;

namespace Sektor.TurnBased.Dialog.Content;

/// <summary>
/// Валидатор контента диалога на загрузке: уникальность узлов, существование всех
/// ссылок (start/next/sub-dialog/continue), корректность флагов и взаимную
/// исключительность вариантов ответа и SubDialogId. Возвращает список всех ошибок.
/// </summary>
public sealed class DialogContentValidator
{
    public Result Validate(DialogContent content)
    {
        if (content is null)
            return Result.Failure("DialogContent cannot be null.");

        var failures = new List<string>();
        var nodeIds = content.Nodes.Select(n => n.Id).ToList();
        var flagIds = content.DeclaredFlags.ToHashSet();

        foreach (var group in content.Nodes.GroupBy(n => n.Id).Where(g => g.Count() > 1))
            failures.Add($"Duplicate node '{group.Key}'.");

        if (!nodeIds.Contains(content.StartNodeId))
            failures.Add($"Start node '{content.StartNodeId}' not found.");

        foreach (var node in content.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Text))
                failures.Add($"Node '{node.Id}' has empty text.");

            if (node.SubDialogId is not null && node.Choices.Count > 0)
                failures.Add($"Node '{node.Id}' cannot have both choices and a sub-dialog.");

            if (node.SubDialogId is not null)
            {
                if (!nodeIds.Contains(node.SubDialogId))
                    failures.Add($"Node '{node.Id}' references unknown sub-dialog '{node.SubDialogId}'.");

                if (string.IsNullOrWhiteSpace(node.ContinueNodeId))
                    failures.Add($"Node '{node.Id}' with a sub-dialog must define ContinueNodeId.");
                else if (!nodeIds.Contains(node.ContinueNodeId))
                    failures.Add($"Node '{node.Id}' references unknown continue node '{node.ContinueNodeId}'.");
            }

            foreach (var group in node.Choices.GroupBy(c => c.Id).Where(g => g.Count() > 1))
                failures.Add($"Node '{node.Id}' has duplicate choice '{group.Key}'.");

            foreach (var choice in node.Choices)
            {
                if (string.IsNullOrWhiteSpace(choice.Text))
                    failures.Add($"Choice '{node.Id}/{choice.Id}' has empty text.");

                if (!nodeIds.Contains(choice.NextNodeId))
                    failures.Add($"Choice '{node.Id}/{choice.Id}' references unknown node '{choice.NextNodeId}'.");

                CheckFlags(node.Id, choice.Id, choice.RequiresFlags, flagIds, failures);
                CheckFlags(node.Id, choice.Id, choice.GrantsFlags, flagIds, failures);
            }

            CheckFlags(node.Id, null, node.RequiresFlags, flagIds, failures);
            CheckFlags(node.Id, null, node.GrantsFlags, flagIds, failures);
        }

        return failures.Count == 0
            ? Result.Success()
            : Result.Failure(string.Join("; ", failures));
    }

    private static void CheckFlags(
        string nodeId,
        string? choiceId,
        IReadOnlyList<string> flags,
        HashSet<string> declared,
        List<string> failures)
    {
        var where = choiceId is null ? $"Node '{nodeId}'" : $"Choice '{nodeId}/{choiceId}'";
        foreach (var flag in flags)
        {
            if (!declared.Contains(flag))
                failures.Add($"{where} references undeclared flag '{flag}'.");
        }
    }
}
