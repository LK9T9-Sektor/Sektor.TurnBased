using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.Dialog.Content;
using Sektor.TurnBased.Dialog.Model;
using Sektor.TurnBased.Dialog.Phases;

namespace Sektor.TurnBased.Dialog;

/// <summary>
/// Фасад диалога: собирает фазы в пайплайн ядра и управляет им.
/// DI через конструктор/факторию; контент валидируется в Create.
/// </summary>
public sealed class DialogEngine
{
    public DialogState State { get; }

    public GamePipeline Pipeline { get; }

    private DialogEngine(DialogState state, GamePipeline pipeline)
    {
        State = state;
        Pipeline = pipeline;
    }

    /// <summary>
    /// Создаёт диалог: валидирует контент и регистрирует фазы.
    /// </summary>
    public static Result<DialogEngine> Create(GameContext context, ContentRegistry content, DialogContent dialogContent)
    {
        if (context is null)
            return Result<DialogEngine>.Failure("GameContext cannot be null.");
        if (content is null)
            return Result<DialogEngine>.Failure("ContentRegistry cannot be null.");
        if (dialogContent is null)
            return Result<DialogEngine>.Failure("DialogContent cannot be null.");

        var validation = new DialogContentValidator().Validate(dialogContent);
        if (validation.IsFailure)
            return Result<DialogEngine>.Failure(validation.Error!);

        var state = new DialogState();
        var sink = new DialogEventSink(context);
        var pipeline = new GamePipeline(context);

        var registerResult = pipeline.Register(new DialogSetupPhase(state, dialogContent));
        if (registerResult.IsFailure)
            return Result<DialogEngine>.Failure(registerResult.Error!);

        registerResult = pipeline.Register(new DialogFlowPhase(state, dialogContent, sink));
        if (registerResult.IsFailure)
            return Result<DialogEngine>.Failure(registerResult.Error!);

        return Result<DialogEngine>.Success(new DialogEngine(state, pipeline));
    }

    public Result Start() => Pipeline.Start(DialogPhaseIds.Setup);

    public Result Advance() => Pipeline.Advance();

    public Result ProcessCommand(IGameCommand command) => Pipeline.ProcessCommand(command);

    public string? CurrentPhaseId => Pipeline.CurrentPhaseId;

    public bool IsSuspended => Pipeline.IsSuspended;

    public bool IsFinished => Pipeline.IsFinished;

    /// <summary>Id узла-концовки или null, если диалог не завершён.</summary>
    public string? Outcome => State.OutcomeNodeId;
}
