using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Battle.Phases;
using Sektor.TurnBased.Battle.Rules;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle;

/// <summary>
/// Фасад боя: собирает фазы в пайплайн ядра и управляет им.
/// DI через конструктор/факторию; всё, что можно сломать на старте, валидируется в Create.
/// </summary>
public sealed class BattleEngine
{
    private readonly GameContext _context;

    public BattleState State { get; }
    public GamePipeline Pipeline { get; }

    private BattleEngine(GameContext context, BattleState state, GamePipeline pipeline)
    {
        _context = context;
        State = state;
        Pipeline = pipeline;
    }

    /// <summary>
    /// Создаёт бой: валидирует контент, резолвит правила и регистрирует фазы.
    /// defaultOrderRules/defaultWinConditions — встроенные стратегии, если не переданы.
    /// </summary>
    public static Result<BattleEngine> Create(
        GameContext context,
        ContentRegistry content,
        BattleContent battleContent,
        BattleConfig config,
        IEnumerable<IOrderRule>? defaultOrderRules = null,
        IEnumerable<IWinCondition>? defaultWinConditions = null)
    {
        if (context is null)
            return Result<BattleEngine>.Failure("GameContext cannot be null.");
        if (content is null)
            return Result<BattleEngine>.Failure("ContentRegistry cannot be null.");
        if (battleContent is null)
            return Result<BattleEngine>.Failure("BattleContent cannot be null.");
        if (config is null)
            return Result<BattleEngine>.Failure("BattleConfig cannot be null.");

        var validation = new ContentValidator().Validate(battleContent, content);
        if (validation.IsFailure)
            return Result<BattleEngine>.Failure(validation.Error!);

        var rulesResult = BattleRules.Create(
            config,
            defaultOrderRules ?? DefaultOrderRules(),
            defaultWinConditions ?? DefaultWinConditions());
        if (rulesResult.IsFailure)
            return Result<BattleEngine>.Failure(rulesResult.Error!);
        if (!rulesResult.TryGetValue(out var rules))
            return Result<BattleEngine>.Failure("Rules could not be resolved.");

        var statDefinitions = battleContent.Stats.ToDictionary(s => s.Id);
        var state = new BattleState(statDefinitions);
        var sink = new BattleEventSink(context, state);
        var executor = new BattleExecutor(context, state, sink, config.CritChance, config.CritMultiplier);
        var ai = new BattleAi(context, state);

        var pipeline = new GamePipeline(context);

        var registerResult = pipeline.Register(new BattleSetupPhase(state, battleContent));
        if (registerResult.IsFailure)
            return Result<BattleEngine>.Failure(registerResult.Error!);

        registerResult = pipeline.Register(new RoundStartPhase(state, rules, sink));
        if (registerResult.IsFailure)
            return Result<BattleEngine>.Failure(registerResult.Error!);

        registerResult = pipeline.Register(new ActorTurnPhase(state, rules, executor, ai, sink));
        if (registerResult.IsFailure)
            return Result<BattleEngine>.Failure(registerResult.Error!);

        registerResult = pipeline.Register(new BattleEndPhase(state));
        if (registerResult.IsFailure)
            return Result<BattleEngine>.Failure(registerResult.Error!);

        return Result<BattleEngine>.Success(new BattleEngine(context, state, pipeline));
    }

    public Result Start() => Pipeline.Start(BattlePhaseIds.Setup);

    public Result Advance() => Pipeline.Advance();

    public Result ProcessCommand(IGameCommand command) => Pipeline.ProcessCommand(command);

    public string? CurrentPhaseId => Pipeline.CurrentPhaseId;

    public bool IsStarted => Pipeline.IsStarted;

    public bool IsSuspended => Pipeline.IsSuspended;

    public bool IsFinished => Pipeline.IsFinished;

    public static IEnumerable<IOrderRule> DefaultOrderRules() =>
        new IOrderRule[]
        {
            new FixedOrderRule("fixed"),
            new SpeedInitiativeRule("initiative"),
            new TeamAlternationRule("alternation"),
        };

    public static IEnumerable<IWinCondition> DefaultWinConditions() =>
        new IWinCondition[]
        {
            new ExterminationCondition("extermination"),
        };
}
