using System.Text.Json;
using Sektor.Network.Abstractions.Lobby;
using Sektor.Network.Abstractions.Messages;
using Sektor.Network.Abstractions.Transport;
using Xunit;

namespace Sektor.Network.Tests;

public class LobbyCoordinatorTests
{
    private static LobbyCoordinator CreateHost(out FakeTransport transport)
    {
        transport = new FakeTransport();
        var coordinator = new LobbyCoordinator(transport);
        transport.Initialize();
        coordinator.CreateLobby("Хост");
        return coordinator;
    }

    [Fact]
    public void CreateLobby_BuildsSnapshotWithHost()
    {
        var coordinator = CreateHost(out _);

        var snapshot = coordinator.Snapshot;

        Assert.NotNull(snapshot);
        Assert.Equal("local_player", snapshot!.HostId);
        Assert.Single(snapshot.Players);
        Assert.Equal("Хост", snapshot.Players[0].Name);
        Assert.True(coordinator.IsHost);
        Assert.True(coordinator.LocalReady);
        Assert.False(snapshot.AllReady);
    }

    [Fact]
    public void ToggleReady_WhenHost_Fails()
    {
        var coordinator = CreateHost(out _);

        var result = coordinator.ToggleReady();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void PlayerJoin_AddsPlayerAndBroadcastsUpdate()
    {
        var coordinator = CreateHost(out var transport);

        transport.SimulatePlayerJoin("remote_1");

        Assert.NotNull(coordinator.Snapshot);
        Assert.Equal(2, coordinator.Snapshot!.Players.Count);
        Assert.Equal("Игрок 2", coordinator.Snapshot.Players[1].Name);

        var update = Assert.Single(transport.SentMessages);
        Assert.Equal(LobbyMessageTypes.LobbyUpdate, update.Type);
        Assert.Contains("remote_1", update.Payload);
    }

    [Fact]
    public void PlayerLeave_RemovesPlayer()
    {
        var coordinator = CreateHost(out var transport);
        transport.SimulatePlayerJoin("remote_1");

        transport.SimulatePlayerLeave("remote_1");

        Assert.NotNull(coordinator.Snapshot);
        Assert.Single(coordinator.Snapshot!.Players);
    }

    [Fact]
    public void ClientJoin_AppliesHostLobbyUpdate()
    {
        var transport = new FakeTransport();
        var coordinator = new LobbyCoordinator(transport);
        transport.Initialize();
        coordinator.JoinLobby("session123", "Клиент");

        var payload = JsonSerializer.Serialize(new LobbyUpdateMessage(
            "host_player", 4,
            new[]
            {
                new LobbyPlayer("host_player", "Хост", true),
                new LobbyPlayer("client_player", "Клиент", false),
            }));

        transport.SimulateReceiveMessage("host_player", LobbyMessageTypes.LobbyUpdate, payload);

        Assert.NotNull(coordinator.Snapshot);
        Assert.Equal("host_player", coordinator.Snapshot!.HostId);
        Assert.Equal(2, coordinator.Snapshot.Players.Count);
        Assert.False(coordinator.IsHost);
    }

    [Fact]
    public void ToggleReady_SendsReadyMessage()
    {
        var transport = new FakeTransport();
        var coordinator = new LobbyCoordinator(transport);
        transport.Initialize();
        coordinator.JoinLobby("session123", "Клиент");

        var result = coordinator.ToggleReady();

        Assert.True(result.IsSuccess);
        Assert.True(coordinator.LocalReady);
        var message = Assert.Single(transport.SentMessages);
        Assert.Equal(LobbyMessageTypes.PlayerReady, message.Type);
    }

    [Fact]
    public void HostReady_SetsRemotePlayerReady()
    {
        var coordinator = CreateHost(out var transport);
        transport.SimulatePlayerJoin("remote_1");

        var payload = JsonSerializer.Serialize(new PlayerReadyMessage(true));
        transport.SimulateReceiveMessage("remote_1", LobbyMessageTypes.PlayerReady, payload);

        Assert.NotNull(coordinator.Snapshot);
        Assert.True(coordinator.Snapshot!.AllReady);
    }

    [Fact]
    public void GameMessage_ForwardedToGameMessageReceived()
    {
        var coordinator = CreateHost(out var transport);
        transport.SimulatePlayerJoin("remote_1");

        TransportMessage? received = null;
        coordinator.GameMessageReceived += msg => received = msg;

        transport.SimulateReceiveMessage("remote_1", "battle_command", "payload");

        Assert.NotNull(received);
        Assert.Equal("battle_command", received!.Type);
        Assert.Equal("remote_1", received.SenderId);
    }

    [Fact]
    public void ProtocolMessage_NotForwarded()
    {
        var coordinator = CreateHost(out var transport);
        transport.SimulatePlayerJoin("remote_1");

        bool forwarded = false;
        coordinator.GameMessageReceived += _ => forwarded = true;

        transport.SimulateReceiveMessage("remote_1", LobbyMessageTypes.PlayerReady,
            JsonSerializer.Serialize(new PlayerReadyMessage(true)));

        Assert.False(forwarded);
    }

    [Fact]
    public void SendToAll_SendsToEachRemote()
    {
        var coordinator = CreateHost(out var transport);
        transport.SimulatePlayerJoin("remote_1");
        transport.SimulatePlayerJoin("remote_2");

        var result = coordinator.SendToAll("custom", "data");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, transport.SentMessages.Count(m => m.Type == "custom"));
    }

    [Fact]
    public void LeaveLobby_ClearsState()
    {
        var coordinator = CreateHost(out _);

        var result = coordinator.LeaveLobby();

        Assert.True(result.IsSuccess);
        Assert.Null(coordinator.Snapshot);
        Assert.False(coordinator.IsSessionActive);
    }
}