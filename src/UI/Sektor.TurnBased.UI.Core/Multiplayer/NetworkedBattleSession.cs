using Sektor.TurnBased.Battle;
using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Battle.Rules;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.UI.Core.Multiplayer;

/// <summary>
/// Сетевая боевая сессия (lockstep): команда игрока рассылается всем участникам,
/// входящие команды буферизуются и применяются локально при Update. Состояние
/// детерминировано: одинаковый seed и та же последовательность команд → тот же бой.
/// </summary>
public sealed class NetworkedBattleSession : BattleSession, INetworkedBattleSession
{
    private readonly BattleCommandChannel _channel;
    private readonly Queue<IGameCommand> _pending = new();

    /// <summary>Состояние изменилось после применения входящей команды.</summary>
    public event Action? StateChanged;

    private NetworkedBattleSession(
        string kind,
        GameContext context,
        BattleEngine engine,
        IReadOnlyDictionary<string, string>? displayNames,
        IReadOnlyList<PlayerPresentation>? presentations,
        int? localSlot,
        BattleCommandChannel channel)
        : base(kind, context, engine, displayNames, presentations, localSlot)
    {
        _channel = channel;
        _channel.CommandReceived += OnRemoteCommand;
    }

    /// <summary>Создаёт сетевой бой из ростера спавнов (слоты игроков + AI).</summary>
    public static Result<NetworkedBattleSession> Create(
        GameContext context,
        ContentRegistry content,
        BattleContent battleContent,
        BattleConfig config,
        IReadOnlyList<BattleSpawn> spawns,
        IReadOnlyList<PlayerPresentation> presentations,
        int? localSlot,
        IReadOnlyDictionary<string, string>? displayNames,
        BattleCommandChannel channel)
    {
        var engineResult = BuildEngine(context, content, battleContent, config, spawns);
        if (engineResult.IsFailure)
            return Result<NetworkedBattleSession>.Failure(engineResult.Error!);

        return Result<NetworkedBattleSession>.Success(
            new NetworkedBattleSession(
                GameKinds.Battle, context, engineResult.Value!, displayNames, presentations, localSlot, channel));
    }

    /// <summary>Рассылает команду всем участникам и применяет её локально.</summary>
    public override Result Submit(IGameCommand command)
    {
        var sent = _channel.Send(command);
        if (sent.IsFailure)
            return sent;
        return base.Submit(command);
    }

    /// <summary>Прокачивает транспорт и применяет буферизованные входящие команды.</summary>
    public void Update()
    {
        _channel.Update();
        TryApplyPending();
    }

    private void OnRemoteCommand(IGameCommand command) => _pending.Enqueue(command);

    private void TryApplyPending()
    {
        while (_pending.Count > 0 && NeedsInput)
        {
            var command = _pending.Dequeue();
            var result = base.Submit(command);
            if (result.IsFailure)
                return;
            StateChanged?.Invoke();
        }
    }
}