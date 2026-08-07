using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Rules;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.Dialog.Content;

namespace Sektor.TurnBased.UI.Core;

/// <summary>
/// Фабрика сессий для лобби: строит контекст ядра с детерминированным RNG (seed)
/// и собирает демо-контент конкретной игры. Диспетчеризация по данным-словарю.
/// </summary>
public static class GameSessionFactory
{
    private sealed class SessionState : IGameState
    {
    }

    private static readonly IReadOnlyDictionary<string, Func<GameContext, ContentRegistry, Result<GameSession>>> Builders =
        new Dictionary<string, Func<GameContext, ContentRegistry, Result<GameSession>>>
        {
            [GameKinds.Battle] = (context, content) => BuildBattle(context, content),
            [GameKinds.Dialog] = (context, content) => BuildDialog(context, content),
        };

    private static readonly IReadOnlyDictionary<string, string> DefaultDisplayNames =
        new Dictionary<string, string>
        {
            ["hero_warrior"] = "Воин",
            ["hero_rogue"] = "Разбойник",
            ["hero_archer"] = "Лучник",
            ["hero_priestess"] = "Жрица",
            ["skeleton"] = "Скелет",
            ["zombie"] = "Зомби",
            ["skeleton_archer"] = "Скелет-лучник",
            ["skeleton_mage"] = "Скелет-маг",
            ["player"] = "Игрок",
            ["enemy"] = "Враг",
        };

    /// <summary>
    /// Создаёт сессию игры по идентификатору с детерминированным seed.
    /// Неизвестная игра — ошибка через Result.
    /// </summary>
    public static Result<GameSession> Create(string gameKind, int seed)
    {
        var content = new ContentRegistry();
        var context = new GameContext(new SessionState(), rng: new DeterministicRng(seed), content: content);

        if (!Builders.TryGetValue(gameKind, out var builder))
            return Result<GameSession>.Failure($"Unknown game kind '{gameKind}'.");

        return builder(context, content);
    }

    private static Result<GameSession> BuildBattle(GameContext context, ContentRegistry content)
    {
        var battleContent = BattleContentCatalog.Build(content);
        if (battleContent.IsFailure)
            return Result<GameSession>.Failure(battleContent.Error!);

        var session = BattleSession.Create(
            context,
            content,
            battleContent.Value!,
            new BattleConfig("initiative", "extermination"),
            DefaultDisplayNames);
        return session.TryGetValue(out var value)
            ? Result<GameSession>.Success(value)
            : Result<GameSession>.Failure(session.Error!);
    }

    private static Result<GameSession> BuildDialog(GameContext context, ContentRegistry content)
    {
        var dialogContent = DialogContentCatalog.Build(content);
        if (dialogContent.IsFailure)
            return Result<GameSession>.Failure(dialogContent.Error!);

        var session = DialogSession.Create(context, content, dialogContent.Value!, DefaultDisplayNames);
        return session.TryGetValue(out var value)
            ? Result<GameSession>.Success(value)
            : Result<GameSession>.Failure(session.Error!);
    }
}
