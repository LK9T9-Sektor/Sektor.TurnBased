using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.Battle.Rules;

/// <summary>
/// Разрешённые правила боя: резолвит стратегии из конфига по Id.
/// Никогда не бросает исключений: отсутствующее правило — ошибка через Result.
/// </summary>
public sealed class BattleRules
{
    public BattleConfig Config { get; }
    public IOrderRule OrderRule { get; }
    public IWinCondition WinCondition { get; }

    private BattleRules(BattleConfig config, IOrderRule orderRule, IWinCondition winCondition)
    {
        Config = config;
        OrderRule = orderRule;
        WinCondition = winCondition;
    }

    public static Result<BattleRules> Create(
        BattleConfig config,
        IEnumerable<IOrderRule> orderRules,
        IEnumerable<IWinCondition> winConditions)
    {
        if (config is null)
            return Result<BattleRules>.Failure("BattleConfig cannot be null.");
        if (orderRules is null)
            return Result<BattleRules>.Failure("Order rules collection cannot be null.");
        if (winConditions is null)
            return Result<BattleRules>.Failure("Win conditions collection cannot be null.");

        var orderRule = orderRules.FirstOrDefault(r => r.Id == config.OrderRuleId);
        if (orderRule is null)
            return Result<BattleRules>.Failure($"Order rule '{config.OrderRuleId}' is not provided.");

        var winCondition = winConditions.FirstOrDefault(w => w.Id == config.WinConditionId);
        if (winCondition is null)
            return Result<BattleRules>.Failure($"Win condition '{config.WinConditionId}' is not provided.");

        return Result<BattleRules>.Success(new BattleRules(config, orderRule, winCondition));
    }
}
