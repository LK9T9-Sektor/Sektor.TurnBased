using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.Dialog.Commands;
using Sektor.TurnBased.Dialog.Content;
using Sektor.TurnBased.Dialog.Phases;
using Xunit;

namespace Sektor.TurnBased.Dialog.Tests;

/// <summary>
/// Тестовый контент: собирает демо-каталог, создаёт контекст и прогоняет диалог
/// по скрипту вариантов до конца. Скрипт подаёт варианты по мере приостановки
/// текущего или вложенного диалога.
/// </summary>
internal static class TestContent
{
    public static (ContentRegistry Content, DialogContent DialogContent) Build()
    {
        var content = new ContentRegistry();
        var result = DialogContentCatalog.Build(content);
        if (result.IsFailure)
            throw new InvalidOperationException($"Test content failed to build: {result.Error}");
        return (content, result.Value!);
    }

    public static GameContext CreateContext(ContentRegistry content, int seed = 1) =>
        new(new EmptyState(), rng: new DeterministicRng(seed), content: content);

    public static DialogEngine CreateEngine(GameContext context, DialogContent dialogContent)
    {
        var result = DialogEngine.Create(context, context.Content, dialogContent);
        if (result.IsFailure)
            throw new InvalidOperationException($"DialogEngine failed to create: {result.Error}");
        return result.Value!;
    }

    /// <summary>Проводит диалог до конца по скрипту вариантов (NodeId, ChoiceId).</summary>
    public static DialogEngine RunToEnd(GameContext context, DialogContent dialogContent, params (string NodeId, string ChoiceId)[] script)
    {
        var engine = CreateEngine(context, dialogContent);
        Assert.True(engine.Start().IsSuccess);

        var scriptIndex = 0;
        while (!engine.IsFinished)
        {
            var advance = engine.Advance();
            Assert.True(advance.IsSuccess, advance.Error ?? "advance failed");

            if (!IsAwaitingChoice(engine))
                continue;

            if (scriptIndex >= script.Length)
                throw new InvalidOperationException("Not enough script steps to finish the dialog.");

            var (nodeId, choiceId) = script[scriptIndex++];
            var command = engine.ProcessCommand(new ChooseOptionCommand(nodeId, choiceId));
            Assert.True(command.IsSuccess, command.Error ?? $"command {nodeId}/{choiceId} failed");
        }

        return engine;
    }

    public static bool IsAwaitingChoice(DialogEngine engine) =>
        (engine.CurrentPhaseId == DialogPhaseIds.Flow && engine.IsSuspended)
        || engine.Pipeline.Children.Any(c => c.CurrentPhaseId == DialogPhaseIds.Flow && c.IsSuspended);

    private sealed class EmptyState : IGameState
    {
    }
}
