using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.Network.Abstractions.Transport;

/// <summary>
/// Network transport abstraction. Provides session lifecycle, messaging, and
/// player tracking. Implementations are responsible for NAT traversal, encryption,
/// and reliable delivery. The transport never throws — all fallible operations
/// return Result.
/// </summary>
public interface ITransport : IDisposable
{
    /// <summary>Fires when the local player joins a session (host or client).</summary>
    event Action<string>? SessionJoined;

    /// <summary>Fires when a remote player joins the session. Payload: player id.</summary>
    event Action<string>? PlayerJoined;

    /// <summary>Fires when a remote player leaves the session. Payload: player id.</summary>
    event Action<string>? PlayerLeft;

    /// <summary>Fires when a message is received from a remote peer.</summary>
    event Action<TransportMessage>? MessageReceived;

    /// <summary>Fires when a Steam-style session invite is received. Payload: session id.</summary>
    event Action<string>? SessionInviteReceived;

    /// <summary>Fires on unexpected disconnect or session loss.</summary>
    event Action? Disconnected;

    /// <summary>Opaque local player id (assigned by the transport provider).</summary>
    string LocalPlayerId { get; }

    /// <summary>Player id of the session host. Empty when not in a session.</summary>
    string HostPlayerId { get; }

    /// <summary>Whether the local player is currently in a session.</summary>
    bool IsSessionActive { get; }

    /// <summary>Initializes the transport provider. Must be called once before any other method.</summary>
    Result Initialize();

    /// <summary>Pumps the transport's callback queue. Must be called every frame.</summary>
    void RunCallbacks();

    /// <summary>Host creates a new session. Fires SessionJoined on success.</summary>
    Result CreateSession(string sessionName, int maxPlayers);

    /// <summary>Client joins an existing session by id. Fires SessionJoined on success.</summary>
    Result JoinSession(string sessionId);

    /// <summary>Leaves the current session.</summary>
    Result LeaveSession();

    /// <summary>Sends a reliable ordered message to all participants.</summary>
    Result SendMessage(string type, string payload);

    /// <summary>Returns all remote player ids currently in the session.</summary>
    string[] GetSessionPlayers();
}
