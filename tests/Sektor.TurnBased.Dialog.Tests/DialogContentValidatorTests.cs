using Sektor.TurnBased.Dialog.Content;
using Sektor.TurnBased.Dialog.Model;
using Xunit;

namespace Sektor.TurnBased.Dialog.Tests;

/// <summary>
/// Тесты валидатора контента: все ошибки ссылок и флагов ловятся на загрузке.
/// </summary>
public class DialogContentValidatorTests
{
    private static DialogContent Content(params DialogNodeDefinition[] nodes) =>
        new(nodes, startNodeId: nodes[0].Id, new[] { "flag_a", "flag_b" });

    [Fact]
    public void ValidContent_Passes()
    {
        var node = Node("a", choices: new[]
        {
            new DialogChoiceDefinition("c1", "Text", NextNodeId: "b", Array.Empty<string>(), Array.Empty<string>()),
        });
        var content = Content(node, Node("b", choices: Array.Empty<DialogChoiceDefinition>()));

        Assert.True(new DialogContentValidator().Validate(content).IsSuccess);
    }

    [Fact]
    public void DuplicateNodeId_Fails()
    {
        var content = Content(Node("a"), Node("a"));

        var result = new DialogContentValidator().Validate(content);

        Assert.True(result.IsFailure);
        Assert.Contains("Duplicate node 'a'", result.Error);
    }

    [Fact]
    public void MissingStartNode_Fails()
    {
        var content = new DialogContent(
            new[] { Node("a") },
            startNodeId: "missing",
            new[] { "flag_a" });

        var result = new DialogContentValidator().Validate(content);

        Assert.True(result.IsFailure);
        Assert.Contains("Start node 'missing' not found", result.Error);
    }

    [Fact]
    public void UnknownChoiceTarget_Fails()
    {
        var node = Node("a", choices: new[]
        {
            new DialogChoiceDefinition("c1", "Text", NextNodeId: "nowhere", Array.Empty<string>(), Array.Empty<string>()),
        });
        var content = Content(node);

        var result = new DialogContentValidator().Validate(content);

        Assert.True(result.IsFailure);
        Assert.Contains("references unknown node 'nowhere'", result.Error);
    }

    [Fact]
    public void UnknownSubDialog_Fails()
    {
        var node = Node("a", subDialogId: "nowhere", continueNodeId: "b");
        var content = Content(node, Node("b", choices: Array.Empty<DialogChoiceDefinition>()));

        var result = new DialogContentValidator().Validate(content);

        Assert.True(result.IsFailure);
        Assert.Contains("references unknown sub-dialog 'nowhere'", result.Error);
    }

    [Fact]
    public void SubDialogWithoutContinue_Fails()
    {
        var node = Node("a", subDialogId: "b");
        var content = Content(node, Node("b", choices: Array.Empty<DialogChoiceDefinition>()));

        var result = new DialogContentValidator().Validate(content);

        Assert.True(result.IsFailure);
        Assert.Contains("must define ContinueNodeId", result.Error);
    }

    [Fact]
    public void ChoicesAndSubDialogTogether_Fails()
    {
        var node = Node("a", choices: new[]
        {
            new DialogChoiceDefinition("c1", "Text", NextNodeId: "b", Array.Empty<string>(), Array.Empty<string>()),
        }, subDialogId: "b", continueNodeId: "b");
        var content = Content(node, Node("b", choices: Array.Empty<DialogChoiceDefinition>()));

        var result = new DialogContentValidator().Validate(content);

        Assert.True(result.IsFailure);
        Assert.Contains("cannot have both choices and a sub-dialog", result.Error);
    }

    [Fact]
    public void DuplicateChoiceId_Fails()
    {
        var node = Node("a", choices: new[]
        {
            new DialogChoiceDefinition("c1", "Text", NextNodeId: "b", Array.Empty<string>(), Array.Empty<string>()),
            new DialogChoiceDefinition("c1", "Text 2", NextNodeId: "b", Array.Empty<string>(), Array.Empty<string>()),
        });
        var content = Content(node, Node("b", choices: Array.Empty<DialogChoiceDefinition>()));

        var result = new DialogContentValidator().Validate(content);

        Assert.True(result.IsFailure);
        Assert.Contains("duplicate choice 'c1'", result.Error);
    }

    [Fact]
    public void UndeclaredFlag_Fails()
    {
        var node = Node("a", requiresFlags: new[] { "nope" });
        var content = Content(node);

        var result = new DialogContentValidator().Validate(content);

        Assert.True(result.IsFailure);
        Assert.Contains("references undeclared flag 'nope'", result.Error);
    }

    [Fact]
    public void EmptyChoiceText_Fails()
    {
        var node = Node("a", choices: new[]
        {
            new DialogChoiceDefinition("c1", "", NextNodeId: "b", Array.Empty<string>(), Array.Empty<string>()),
        });
        var content = Content(node, Node("b", choices: Array.Empty<DialogChoiceDefinition>()));

        var result = new DialogContentValidator().Validate(content);

        Assert.True(result.IsFailure);
        Assert.Contains("has empty text", result.Error);
    }

    private static DialogNodeDefinition Node(
        string id,
        IReadOnlyList<DialogChoiceDefinition>? choices = null,
        IReadOnlyList<string>? requiresFlags = null,
        IReadOnlyList<string>? grantsFlags = null,
        string? subDialogId = null,
        string? continueNodeId = null) =>
        new(
            id,
            "Text",
            choices ?? Array.Empty<DialogChoiceDefinition>(),
            requiresFlags ?? Array.Empty<string>(),
            grantsFlags ?? Array.Empty<string>(),
            subDialogId,
            continueNodeId);
}
