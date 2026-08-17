using Sektor.Network.Abstractions.Transport;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.Network.Tests;

/// <summary>
/// Fake transport for unit testing. Simulates session lifecycle and messaging
/// without a real network provider.
/// </summary>
public sealed class FakeTransport : ITransport
{
    private readonly List<string> _remotePlayers = [];
    private readonly List<TransportMessage> _sentMessages = [];

    public event Action<string>? SessionJoined;
    public event Action<string>? PlayerJoined;
    public event Action<string>? PlayerLeft;
    public event Action<TransportMessage>? MessageReceived;
    public event Action<string>? SessionInviteReceived;
    public event Action? Disconnected;

    public string LocalPlayerId { get; } = "local_player";
    public string HostPlayerId { get; private set; } = string.Empty;
    public bool IsSessionActive { get; private set; }

    public IReadOnlyList<TransportMessage> SentMessages => _sentMessages;
    public IReadOnlyList<string> RemotePlayers => _remotePlayers;

    public Result Initialize()
    {
        return Result.Success();
    }

    public void RunCallbacks()
    {
    }

    public Result CreateSession(string sessionName, int maxPlayers)
    {
        if (IsSessionActive)
            return Result.Failure("Already in a session.");

        IsSessionActive = true;
        HostPlayerId = LocalPlayerId;
        SessionJoined?.Invoke("test_session");
        return Result.Success();
    }

    public Result JoinSession(string sessionId)
    {
        if (IsSessionActive)
            return Result.Failure("Already in a session.");

        IsSessionActive = true;
        HostPlayerId = "remote_host";
        _remotePlayers.Add("remote_host");
        PlayerJoined?.Invoke("remote_host");
        SessionJoined?.Invoke(sessionId);
        return Result.Success();
    }

    public Result LeaveSession()
    {
        if (!IsSessionActive)
            return Result.Failure("Not in a session.");

        IsSessionActive = false;
        HostPlayerId = string.Empty;
        _remotePlayers.Clear();
        return Result.Success();
    }

    public Result SendMessage(string type, string payload)
    {
        if (!IsSessionActive)
            return Result.Failure("Not in a session.");

        var message = new TransportMessage(LocalPlayerId, type, payload);
        _sentMessages.Add(message);
        return Result.Success();
    }

    public string[] GetSessionPlayers()
    {
        return [.. _remotePlayers];
    }

    /// <summary>Simulates receiving a message from a remote peer.</summary>
    public void SimulateReceiveMessage(string senderId, string type, string payload)
    {
        var message = new TransportMessage(senderId, type, payload);
        MessageReceived?.Invoke(message);
    }

    /// <summary>Simulates a remote player joining.</summary>
    public void SimulatePlayerJoin(string playerId)
    {
        _remotePlayers.Add(playerId);
        PlayerJoined?.Invoke(playerId);
    }

    /// <summary>Simulates a remote player leaving.</summary>
    public void SimulatePlayerLeave(string playerId)
    {
        _remotePlayers.Remove(playerId);
        PlayerLeft?.Invoke(playerId);
    }

    /// <summary>Simulates a disconnect.</summary>
    public void SimulateDisconnect()
    {
        Disconnected?.Invoke();
    }

    public void Dispose()
    {
        if (IsSessionActive)
        {
            IsSessionActive = false;
            _remotePlayers.Clear();
        }
    }
}
