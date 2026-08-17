using Sektor.Network.Abstractions.Lobby;
using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Battle.Rules;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.Dialog.Content;
using Sektor.TurnBased.UI.Core.Multiplayer;

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
            [GameKinds.Battle] = (context, content) => BuildBattle(GameKinds.Battle, context, content),
            [GameKinds.BattleLine] = (context, content) => BuildBattle(GameKinds.BattleLine, context, content),
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

    /// <summary>
    /// Создаёт мультиплеерный бой (lockstep): один seed у всех клиентов, ростер из
    /// назначений (герои по слотам player_N) плюс AI-враги из каталога. Команды
    /// идут через BattleCommandChannel координатора лобби.
    /// </summary>
    public static Result<GameSession> CreateMultiplayerBattle(
        int seed,
        IReadOnlyList<PlayerAssignment> assignments,
        IReadOnlyList<PlayerPresentation> presentations,
        LobbyCoordinator coordinator,
        int? localSlot = null)
    {
        var content = new ContentRegistry();
        var context = new GameContext(new SessionState(), rng: new DeterministicRng(seed), content: content);

        var battleContent = BattleContentCatalog.Build(content);
        if (battleContent.IsFailure)
            return Result<GameSession>.Failure(battleContent.Error!);

        var spawns = BuildSpawns(assignments, battleContent.Value!);
        var channel = new BattleCommandChannel(coordinator);
        var session = NetworkedBattleSession.Create(
            context,
            content,
            battleContent.Value!,
            new BattleConfig("initiative", "extermination", CritChance: 0.15, CritMultiplier: 1.5),
            spawns,
            presentations,
            localSlot,
            DefaultDisplayNames,
            channel);
        return session.TryGetValue(out var value)
            ? Result<GameSession>.Success(value)
            : Result<GameSession>.Failure(session.Error!);
    }

    private static IReadOnlyList<BattleSpawn> BuildSpawns(
        IReadOnlyList<PlayerAssignment> assignments,
        BattleContent battleContent)
    {
        var spawns = new List<BattleSpawn>();
        for (int i = 0; i < assignments.Count; i++)
            spawns.Add(new BattleSpawn(assignments[i].ClassId, "player", $"player_{i}"));
        foreach (var template in battleContent.Templates)
        {
            if (template.ControlledBy == "ai")
                spawns.Add(new BattleSpawn(template.Id, template.TeamId, template.ControlledBy));
        }
        return spawns;
    }

    private static Result<GameSession> BuildBattle(string kind, GameContext context, ContentRegistry content)
    {
        var battleContent = BattleContentCatalog.Build(content);
        if (battleContent.IsFailure)
            return Result<GameSession>.Failure(battleContent.Error!);

        var session = BattleSession.Create(
            context,
            content,
            battleContent.Value!,
            new BattleConfig("initiative", "extermination", CritChance: 0.15, CritMultiplier: 1.5),
            DefaultDisplayNames,
            kind);
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
