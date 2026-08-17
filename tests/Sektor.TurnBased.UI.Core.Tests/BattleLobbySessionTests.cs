using System.Text.Json;
using Sektor.Network.Abstractions.Lobby;
using Sektor.Network.Abstractions.Messages;
using Sektor.Network.Abstractions.Transport;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.UI.Core.Multiplayer;
using Xunit;

namespace Sektor.TurnBased.UI.Core.Tests;

public class BattleLobbySessionTests
{
    private static readonly string[] Heroes = ["hero_warrior", "hero_rogue", "hero_archer"];

    private static BattleLobbySession CreateClient(LobbyCoordinator coordinator)
    {
        return new BattleLobbySession(coordinator, Heroes);
    }

    [Fact]
    public void SelectClassRight_FromEmpty_SelectsFirstHeroAndSends()
    {
        var transport = new SessionTransportFake();
        var coordinator = new LobbyCoordinator(transport);
        transport.Initialize();
        coordinator.JoinLobby("session", "Клиент");
        var session = CreateClient(coordinator);

        var result = session.SelectClassRight();

        Assert.True(result.IsSuccess);
        Assert.Equal("hero_warrior", session.LocalClassId);
        var message = Assert.Single(transport.SentMessages);
        Assert.Equal(BattleMessageTypes.PlayerSelectClass, message.Type);
        Assert.Contains("hero_warrior", message.Payload);
    }

    [Fact]
    public void SelectClassLeft_FromEmpty_WrapsToLastHero()
    {
        var transport = new SessionTransportFake();
        var coordinator = new LobbyCoordinator(transport);
        transport.Initialize();
        coordinator.JoinLobby("session", "Клиент");
        var session = CreateClient(coordinator);

        session.SelectClassLeft();

        Assert.Equal("hero_archer", session.LocalClassId);
    }

    [Fact]
    public void StartGame_WhenNotHost_Fails()
    {
        var transport = new SessionTransportFake();
        var coordinator = new LobbyCoordinator(transport);
        transport.Initialize();
        coordinator.JoinLobby("session", "Клиент");
        var session = CreateClient(coordinator);

        var result = session.StartGame();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void StartGame_WhenNotAllReady_Fails()
    {
        var transport = new SessionTransportFake();
        var coordinator = new LobbyCoordinator(transport);
        transport.Initialize();
        coordinator.CreateLobby("Хост");
        transport.SimulatePlayerJoin("client");
        var session = CreateClient(coordinator);

        var result = session.StartGame();

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void StartGame_BuildsAssignmentsAndBroadcasts()
    {
        var transport = new SessionTransportFake();
        var coordinator = new LobbyCoordinator(transport);
        transport.Initialize();
        coordinator.CreateLobby("Хост");
        transport.SimulatePlayerJoin("client");
        MarkReady(transport, coordinator, "client");
        var session = CreateClient(coordinator);

        var result = session.StartGame();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, session.Assignments!.Count);
        Assert.Equal("hero_warrior", session.Assignments[0].ClassId);
        Assert.Equal("hero_rogue", session.Assignments[1].ClassId);
        Assert.Contains(transport.SentMessages,
            m => m.Type == BattleMessageTypes.StartGame && m.Payload.Contains("hero_warrior"));
    }

    [Fact]
    public void StartGame_UsesSelectedClasses()
    {
        var transport = new SessionTransportFake();
        var coordinator = new LobbyCoordinator(transport);
        transport.Initialize();
        coordinator.CreateLobby("Хост");
        transport.SimulatePlayerJoin("client");
        MarkReady(transport, coordinator, "client");
        var session = CreateClient(coordinator);

        var selectPayload = JsonSerializer.Serialize(new SelectClassMessage("hero_archer"));
        transport.SimulateReceive("client", BattleMessageTypes.PlayerSelectClass, selectPayload);
        session.StartGame();

        Assert.Equal("hero_archer", session.PlayerClassId("client"));
        Assert.Equal("hero_warrior", session.PlayerClassId("local"));
    }

    [Fact]
    public void Client_AppliesStartGameAssignments()
    {
        var transport = new SessionTransportFake();
        var coordinator = new LobbyCoordinator(transport);
        transport.Initialize();
        coordinator.JoinLobby("session", "Клиент");
        var session = CreateClient(coordinator);

        var payload = JsonSerializer.Serialize(new StartGameMessage(
            42,
            new[]
            {
                new PlayerAssignment("host", "hero_warrior"),
                new PlayerAssignment("client", "hero_archer"),
            }));
        transport.SimulateReceive("host", BattleMessageTypes.StartGame, payload);

        Assert.NotNull(session.Assignments);
        Assert.Equal(2, session.Assignments!.Count);
        Assert.Equal("hero_archer", session.PlayerClassId("client"));
    }

    [Fact]
    public void PlayerClassId_FallsBackByRosterIndex()
    {
        var transport = new SessionTransportFake();
        var coordinator = new LobbyCoordinator(transport);
        transport.Initialize();
        coordinator.CreateLobby("Хост");
        transport.SimulatePlayerJoin("client");
        var session = CreateClient(coordinator);

        Assert.Equal("hero_warrior", session.PlayerClassId("local"));
        Assert.Equal("hero_rogue", session.PlayerClassId("client"));
    }

    private static void MarkReady(SessionTransportFake transport, LobbyCoordinator coordinator, string playerId)
    {
        var payload = JsonSerializer.Serialize(new PlayerReadyMessage(true));
        transport.SimulateReceive(playerId, LobbyMessageTypes.PlayerReady, payload);
        Assert.True(coordinator.Snapshot!.AllReady);
    }

    private sealed class SessionTransportFake : ITransport
    {
        private readonly List<TransportMessage> _sent = [];
        private bool _isHost;
        private bool _active;

        public event Action<string>? SessionJoined;
        public event Action<string>? PlayerJoined;
        public event Action<string>? PlayerLeft;
        public event Action<TransportMessage>? MessageReceived;
        public event Action? Disconnected;

        public string LocalPlayerId { get; } = "local";
        public string HostPlayerId { get; private set; } = string.Empty;
        public bool IsSessionActive => _active;

        public IReadOnlyList<TransportMessage> SentMessages => _sent;

        public Result Initialize() => Result.Success();

        public void RunCallbacks()
        {
        }

        public Result CreateSession(string sessionName, int maxPlayers)
        {
            _active = true;
            _isHost = true;
            HostPlayerId = LocalPlayerId;
            SessionJoined?.Invoke("session");
            return Result.Success();
        }

        public Result JoinSession(string sessionId)
        {
            _active = true;
            _isHost = false;
            HostPlayerId = "host";
            SessionJoined?.Invoke(sessionId);
            return Result.Success();
        }

        public Result LeaveSession()
        {
            _active = false;
            HostPlayerId = string.Empty;
            return Result.Success();
        }

        public Result SendMessage(string type, string payload)
        {
            _sent.Add(new TransportMessage(LocalPlayerId, type, payload));
            return Result.Success();
        }

        public string[] GetSessionPlayers() => _isHost ? ["client"] : ["host"];

        public void SimulatePlayerJoin(string playerId)
        {
            PlayerJoined?.Invoke(playerId);
        }

        public void SimulatePlayerLeave(string playerId)
        {
            PlayerLeft?.Invoke(playerId);
        }

        public void SimulateDisconnect()
        {
            Disconnected?.Invoke();
        }

        public void SimulateReceive(string senderId, string type, string payload)
        {
            MessageReceived?.Invoke(new TransportMessage(senderId, type, payload));
        }

        public void Dispose()
        {
        }
    }
}