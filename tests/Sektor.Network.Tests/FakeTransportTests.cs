using Sektor.Network.Abstractions.Transport;
using Xunit;

namespace Sektor.Network.Tests;

public class FakeTransportTests
{
    [Fact]
    public void Initialize_ReturnsSuccess()
    {
        using var transport = new FakeTransport();
        var result = transport.Initialize();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CreateSession_SetsSessionActive()
    {
        using var transport = new FakeTransport();
        transport.Initialize();

        var result = transport.CreateSession("test", 4);

        Assert.True(result.IsSuccess);
        Assert.True(transport.IsSessionActive);
        Assert.Equal("local_player", transport.HostPlayerId);
    }

    [Fact]
    public void CreateSession_WhenAlreadyActive_ReturnsFailure()
    {
        using var transport = new FakeTransport();
        transport.Initialize();
        transport.CreateSession("test", 4);

        var result = transport.CreateSession("test2", 4);

        Assert.True(result.IsFailure);
        Assert.Contains("Already in a session", result.Error);
    }

    [Fact]
    public void JoinSession_SetsSessionActive()
    {
        using var transport = new FakeTransport();
        transport.Initialize();

        var result = transport.JoinSession("session123");

        Assert.True(result.IsSuccess);
        Assert.True(transport.IsSessionActive);
        Assert.Equal("remote_host", transport.HostPlayerId);
    }

    [Fact]
    public void LeaveSession_ClearsState()
    {
        using var transport = new FakeTransport();
        transport.Initialize();
        transport.CreateSession("test", 4);

        var result = transport.LeaveSession();

        Assert.True(result.IsSuccess);
        Assert.False(transport.IsSessionActive);
        Assert.Equal(string.Empty, transport.HostPlayerId);
    }

    [Fact]
    public void LeaveSession_WhenNotActive_ReturnsFailure()
    {
        using var transport = new FakeTransport();
        transport.Initialize();

        var result = transport.LeaveSession();

        Assert.True(result.IsFailure);
        Assert.Contains("Not in a session", result.Error);
    }

    [Fact]
    public void SendMessage_RecordsMessage()
    {
        using var transport = new FakeTransport();
        transport.Initialize();
        transport.CreateSession("test", 4);

        var result = transport.SendMessage("test_type", "test_payload");

        Assert.True(result.IsSuccess);
        Assert.Single(transport.SentMessages);
        Assert.Equal("test_type", transport.SentMessages[0].Type);
        Assert.Equal("test_payload", transport.SentMessages[0].Payload);
    }

    [Fact]
    public void SendMessage_WhenNotActive_ReturnsFailure()
    {
        using var transport = new FakeTransport();
        transport.Initialize();

        var result = transport.SendMessage("test_type", "test_payload");

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void SessionJoined_FiresOnCreate()
    {
        using var transport = new FakeTransport();
        transport.Initialize();
        string? joinedSessionId = null;
        transport.SessionJoined += id => joinedSessionId = id;

        transport.CreateSession("test", 4);

        Assert.Equal("test_session", joinedSessionId);
    }

    [Fact]
    public void PlayerJoined_FiresOnSimulateJoin()
    {
        using var transport = new FakeTransport();
        transport.Initialize();
        string? joinedPlayer = null;
        transport.PlayerJoined += id => joinedPlayer = id;

        transport.SimulatePlayerJoin("player2");

        Assert.Equal("player2", joinedPlayer);
        Assert.Contains("player2", transport.RemotePlayers);
    }

    [Fact]
    public void PlayerLeft_FiresOnSimulateLeave()
    {
        using var transport = new FakeTransport();
        transport.Initialize();
        transport.SimulatePlayerJoin("player2");
        string? leftPlayer = null;
        transport.PlayerLeft += id => leftPlayer = id;

        transport.SimulatePlayerLeave("player2");

        Assert.Equal("player2", leftPlayer);
        Assert.DoesNotContain("player2", transport.RemotePlayers);
    }

    [Fact]
    public void MessageReceived_FiresOnSimulateReceive()
    {
        using var transport = new FakeTransport();
        transport.Initialize();
        TransportMessage? received = null;
        transport.MessageReceived += msg => received = msg;

        transport.SimulateReceiveMessage("remote_player", "ping", "data");

        Assert.NotNull(received);
        Assert.Equal("remote_player", received.SenderId);
        Assert.Equal("ping", received.Type);
        Assert.Equal("data", received.Payload);
    }

    [Fact]
    public void Disconnected_FiresOnSimulateDisconnect()
    {
        using var transport = new FakeTransport();
        transport.Initialize();
        bool disconnected = false;
        transport.Disconnected += () => disconnected = true;

        transport.SimulateDisconnect();

        Assert.True(disconnected);
    }

    [Fact]
    public void GetSessionPlayers_ReturnsRemotePlayers()
    {
        using var transport = new FakeTransport();
        transport.Initialize();
        transport.SimulatePlayerJoin("player1");
        transport.SimulatePlayerJoin("player2");

        string[] players = transport.GetSessionPlayers();

        Assert.Equal(2, players.Length);
        Assert.Contains("player1", players);
        Assert.Contains("player2", players);
    }
}
