using Sektor.Network.Abstractions.Lobby;
using Sektor.Network.Abstractions.Transport;
using Sektor.TurnBased.Battle.Commands;
using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Battle.Rules;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.UI.Core;
using Sektor.TurnBased.UI.Core.Multiplayer;
using Xunit;

namespace Sektor.TurnBased.UI.Core.Tests;

/// <summary>
/// Тесты сетевого боя (lockstep): реле-транспорт, канал команд, применение входящих
/// команд и мультиплеерные атрибуты снапшотов (слоты, имена, цвета, локальный ход).
/// </summary>
public class NetworkedBattleTests
{
    private sealed class EmptyState : IGameState
    {
    }

    [Fact]
    public void StartGame_HostBroadcastsSeedAndAssignments_ClientRaisesGameStarted()
    {
        var (hostTransport, clientTransport) = RelayTransport.CreatePair();
        var hostCoordinator = new LobbyCoordinator(hostTransport);
        var clientCoordinator = new LobbyCoordinator(clientTransport);
        var hostLobby = new BattleLobbySession(hostCoordinator);
        var clientLobby = new BattleLobbySession(clientCoordinator);

        hostTransport.Initialize();
        clientTransport.Initialize();
        hostCoordinator.CreateLobby("Хост");
        clientCoordinator.JoinLobby("lobby", "Клиент");
        clientCoordinator.ToggleReady();

        var raised = false;
        clientLobby.GameStarted += () => raised = true;

        var started = hostLobby.StartGame();

        Assert.True(started.IsSuccess, started.Error);
        Assert.NotNull(hostLobby.Seed);
        Assert.Equal(2, hostLobby.Assignments!.Count);
        Assert.True(raised);
        Assert.Equal(hostLobby.Seed, clientLobby.Seed);
        Assert.Equal(hostLobby.Assignments.Count, clientLobby.Assignments!.Count);
    }

    [Fact]
    public void Submit_BroadcastsCommandAndReceiverApplies_IdenticalResult()
    {
        var (hostTransport, clientTransport) = RelayTransport.CreatePair();
        var hostCoordinator = new LobbyCoordinator(hostTransport);
        var clientCoordinator = new LobbyCoordinator(clientTransport);
        hostTransport.Initialize();
        clientTransport.Initialize();
        hostCoordinator.CreateLobby("Хост");
        clientCoordinator.JoinLobby("lobby", "Клиент");

        var assignments = new[]
        {
            new PlayerAssignment("host", "hero_rogue"),
            new PlayerAssignment("client", "hero_warrior"),
        };
        var presentations = new[]
        {
            new PlayerPresentation("Хост", "#FF4444"),
            new PlayerPresentation("Клиент", "#44FF44"),
        };
        var (hostSession, clientSession) = BuildSessionPair(
            hostCoordinator, clientCoordinator, assignments, presentations, hostSlot: 0, clientSlot: 1);

        Assert.True(hostSession.Start().IsSuccess);
        Assert.True(clientSession.Start().IsSuccess);
        Assert.True(hostSession.Snapshot().IsLocalTurn, "Хост (rogue, init 12) должен ходить первым.");
        Assert.False(clientSession.Snapshot().IsLocalTurn);

        var snap = hostSession.Snapshot();
        var target = snap.Actors.First(a => a.TeamId == "enemy");
        var action = snap.AvailableActions.First(a => a.TargetMode == BattleTargetModes.SingleEnemy);
        var command = new UseActionCommand(snap.CurrentActorId!, action.ActionId, new[] { target.RuntimeId });

        var submitted = hostSession.Submit(command);
        Assert.True(submitted.IsSuccess, submitted.Error);

        clientSession.Update();

        Assert.False(clientSession.IsFailed, clientSession.Error);
        Assert.Equal(hostSession.Log, clientSession.Log);
        Assert.Equal(hostSession.Snapshot().RoundNumber, clientSession.Snapshot().RoundNumber);
        Assert.Equal(hostSession.Snapshot().TurnIndex, clientSession.Snapshot().TurnIndex);
    }

    [Fact]
    public void MultiplayerBattle_SnapshotHasPlayerNamesAndColors_AndGatesActionsByLocalSlot()
    {
        var assignments = new[]
        {
            new PlayerAssignment("host", "hero_warrior"),
            new PlayerAssignment("client", "hero_warrior"),
        };
        var presentations = new[]
        {
            new PlayerPresentation("Хост", "#FF4444"),
            new PlayerPresentation("Клиент", "#44FF44"),
        };

        var hostCreated = GameSessionFactory.CreateMultiplayerBattle(42, assignments, presentations, TestCoordinator(), localSlot: 0);
        var clientCreated = GameSessionFactory.CreateMultiplayerBattle(42, assignments, presentations, TestCoordinator(), localSlot: 1);
        Assert.True(hostCreated.IsSuccess, hostCreated.Error);
        Assert.True(clientCreated.IsSuccess, clientCreated.Error);

        var hostSession = (BattleSession)hostCreated.Value!;
        var clientSession = (BattleSession)clientCreated.Value!;
        Assert.True(hostSession.Start().IsSuccess);
        Assert.True(clientSession.Start().IsSuccess);

        var hostSnap = hostSession.Snapshot();
        var clientSnap = clientSession.Snapshot();
        Assert.Equal(hostSnap.CurrentActorId, clientSnap.CurrentActorId);

        var current = hostSnap.Actors.First(a => a.RuntimeId == hostSnap.CurrentActorId);
        var currentSlot = int.Parse(current.ControlledBy["player_".Length..]);

        Assert.Equal(currentSlot == 0, hostSnap.IsLocalTurn);
        Assert.Equal(currentSlot == 1, clientSnap.IsLocalTurn);
        Assert.Equal(hostSnap.AvailableActions.Count > 0, hostSnap.IsLocalTurn);
        Assert.Equal(clientSnap.AvailableActions.Count > 0, clientSnap.IsLocalTurn);

        var hero = hostSnap.Actors.First(a => a.TeamId == "player" && a.ControlledBy == $"player_{currentSlot}");
        Assert.Equal(presentations[currentSlot].Name, hero.PlayerName);
        Assert.Equal(presentations[currentSlot].ColorHex, hero.PlayerColorHex);
    }

    private static (NetworkedBattleSession Host, NetworkedBattleSession Client) BuildSessionPair(
        LobbyCoordinator hostCoordinator,
        LobbyCoordinator clientCoordinator,
        IReadOnlyList<PlayerAssignment> assignments,
        IReadOnlyList<PlayerPresentation> presentations,
        int hostSlot,
        int clientSlot)
    {
        var (host, _) = BuildSession(hostCoordinator, assignments, presentations, hostSlot);
        var (client, _) = BuildSession(clientCoordinator, assignments, presentations, clientSlot);
        return (host, client);
    }

    private static (NetworkedBattleSession Session, int Slot) BuildSession(
        LobbyCoordinator coordinator,
        IReadOnlyList<PlayerAssignment> assignments,
        IReadOnlyList<PlayerPresentation> presentations,
        int localSlot)
    {
        var content = new ContentRegistry();
        var build = BattleContentCatalog.Build(content);
        Assert.True(build.IsSuccess, build.Error);
        var battleContent = TestHelpers.FilterTemplates(build.Value!, "hero_warrior", "hero_rogue", "skeleton");
        var context = new GameContext(new EmptyState(), rng: new DeterministicRng(42), content: content);

        var spawns = new List<BattleSpawn>();
        for (int i = 0; i < assignments.Count; i++)
            spawns.Add(new BattleSpawn(assignments[i].ClassId, "player", $"player_{i}"));
        foreach (var template in battleContent.Templates)
        {
            if (template.ControlledBy == "ai")
                spawns.Add(new BattleSpawn(template.Id, template.TeamId, template.ControlledBy));
        }

        var channel = new BattleCommandChannel(coordinator);
        var created = NetworkedBattleSession.Create(
            context, content, battleContent,
            new BattleConfig("initiative", "extermination"),
            spawns, presentations, localSlot, null, channel);
        Assert.True(created.IsSuccess, created.Error);
        return (created.Value!, localSlot);
    }

    private static LobbyCoordinator TestCoordinator()
    {
        var transport = new RelayTransport($"player_{Guid.NewGuid():N}", null);
        var coordinator = new LobbyCoordinator(transport);
        transport.Initialize();
        return coordinator;
    }

    /// <summary>Реле-транспорт для пары «хост-клиент»: сообщения доставляются пиру напрямую.</summary>
    private sealed class RelayTransport : ITransport
    {
        private readonly string _localId;
        private RelayTransport? _peer;
        private readonly List<string> _remotePlayers = [];
        private bool _active;

        public event Action<string>? SessionJoined;
        public event Action<string>? PlayerJoined;
        public event Action<string>? PlayerLeft;
        public event Action<TransportMessage>? MessageReceived;
        public event Action? Disconnected;

        public string LocalPlayerId => _localId;
        public string HostPlayerId { get; private set; } = string.Empty;
        public bool IsSessionActive => _active;

        public RelayTransport(string localId, RelayTransport? peer)
        {
            _localId = localId;
            _peer = peer;
        }

        public static (RelayTransport Host, RelayTransport Client) CreatePair()
        {
            var host = new RelayTransport("host", null);
            var client = new RelayTransport("client", host);
            host._peer = client;
            return (host, client);
        }

        public Result Initialize() => Result.Success();

        public void RunCallbacks()
        {
        }

        public Result CreateSession(string sessionName, int maxPlayers)
        {
            _active = true;
            HostPlayerId = _localId;
            SessionJoined?.Invoke(sessionName);
            return Result.Success();
        }

        public Result JoinSession(string sessionId)
        {
            _active = true;
            HostPlayerId = _peer?._localId ?? "host";
            _remotePlayers.Add(HostPlayerId);
            _peer?.OnRemoteJoined(_localId);
            SessionJoined?.Invoke(sessionId);
            return Result.Success();
        }

        public Result LeaveSession()
        {
            _active = false;
            HostPlayerId = string.Empty;
            _remotePlayers.Clear();
            _peer?.OnRemoteLeft(_localId);
            return Result.Success();
        }

        public Result SendMessage(string type, string payload)
        {
            if (!_active)
                return Result.Failure("Not in a session.");
            _peer?.Receive(_localId, type, payload);
            return Result.Success();
        }

        public string[] GetSessionPlayers() =>
            _active && _peer is { IsSessionActive: true } ? [_peer._localId] : [];

        private void OnRemoteJoined(string playerId)
        {
            _remotePlayers.Add(playerId);
            PlayerJoined?.Invoke(playerId);
        }

        private void OnRemoteLeft(string playerId)
        {
            _remotePlayers.Remove(playerId);
            PlayerLeft?.Invoke(playerId);
        }

        private void Receive(string senderId, string type, string payload)
        {
            MessageReceived?.Invoke(new TransportMessage(senderId, type, payload));
        }

        /// <summary>Симулирует отключение (для полноты ITransport).</summary>
        public void SimulateDisconnect() => Disconnected?.Invoke();

        public void Dispose()
        {
        }
    }
}