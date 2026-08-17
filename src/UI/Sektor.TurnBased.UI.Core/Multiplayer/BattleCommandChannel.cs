using System.Text.Json;
using Sektor.Network.Abstractions.Lobby;
using Sektor.Network.Abstractions.Transport;
using Sektor.TurnBased.Battle.Commands;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.UI.Core.Multiplayer;

/// <summary>
/// Канал команд боя поверх LobbyCoordinator: сериализует IGameCommand в
/// battle_command, рассылает всем участникам и доставляет входящие команды в
/// CommandReceived. Lockstep-минимум: сам клиент применяет команду локально.
/// </summary>
public sealed class BattleCommandChannel
{
    private readonly LobbyCoordinator _coordinator;

    /// <summary>Входящая команда боя от удалённого игрока.</summary>
    public event Action<IGameCommand>? CommandReceived;

    /// <summary>Создаёт канал команд поверх координатора.</summary>
    public BattleCommandChannel(LobbyCoordinator coordinator)
    {
        _coordinator = coordinator;
        _coordinator.GameMessageReceived += OnGameMessage;
    }

    /// <summary>Рассылает команду боя всем удалённым участникам.</summary>
    public Result Send(IGameCommand command)
    {
        var payload = JsonSerializer.Serialize(ToMessage(command));
        return _coordinator.SendToAll(BattleMessageTypes.BattleCommand, payload);
    }

    /// <summary>Прокачивает транспорт (входящие сообщения → CommandReceived).</summary>
    public void Update() => _coordinator.Update();

    private void OnGameMessage(TransportMessage message)
    {
        if (message.Type != BattleMessageTypes.BattleCommand)
            return;

        var msg = JsonSerializer.Deserialize<BattleCommandMessage>(message.Payload);
        if (msg is null)
            return;

        CommandReceived?.Invoke(ToCommand(msg));
    }

    private static BattleCommandMessage ToMessage(IGameCommand command) =>
        command switch
        {
            UseActionCommand use => new BattleCommandMessage(use.ActorRuntimeId, use.ActionId, use.TargetActorIds),
            SkipTurnCommand skip => new BattleCommandMessage(skip.ActorRuntimeId, null, []),
            _ => throw new InvalidOperationException($"Unsupported command type '{command.GetType().Name}'."),
        };

    private static IGameCommand ToCommand(BattleCommandMessage message) =>
        message.ActionId is null
            ? new SkipTurnCommand(message.ActorRuntimeId)
            : new UseActionCommand(message.ActorRuntimeId, message.ActionId, message.TargetActorIds);
}