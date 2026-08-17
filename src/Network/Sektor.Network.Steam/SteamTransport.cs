using System.Text;
using Sektor.Network.Abstractions.Transport;
using Sektor.TurnBased.Core.Abstractions;
using Steamworks;

namespace Sektor.Network.Steam;

/// <summary>
/// Steam transport implementation. Uses Steam lobbies for session management
/// and Steam P2P for reliable messaging. All errors return Result — no exceptions.
/// </summary>
public sealed class SteamTransport : ITransport
{
    private const string SteamAppIdEnvVar = "SteamAppId";
    private const string LobbyDataHostSteamId = "host_steam_id";
    private const int GameChannel = 1;
    private const int ReceiveBufferSize = 65535;

    private readonly ITransportCodec _codec;

    private readonly Callback<LobbyCreated_t> _lobbyCreatedCallback;
    private readonly Callback<GameLobbyJoinRequested_t> _lobbyJoinRequestedCallback;
    private readonly Callback<LobbyEnter_t> _lobbyEnterCallback;
    private readonly Callback<LobbyChatUpdate_t> _lobbyChatUpdateCallback;
    private readonly Callback<P2PSessionRequest_t> _p2pSessionRequestCallback;
    private readonly Callback<P2PSessionConnectFail_t> _p2pConnectFailCallback;

    private CSteamID _currentLobbyId;

    /// <inheritdoc />
    public event Action<string>? SessionJoined;

    /// <inheritdoc />
    public event Action<string>? PlayerJoined;

    /// <inheritdoc />
    public event Action<string>? PlayerLeft;

    /// <inheritdoc />
    public event Action<TransportMessage>? MessageReceived;

    /// <inheritdoc />
    public event Action<string>? SessionInviteReceived;

    /// <inheritdoc />
    public event Action? Disconnected;

    /// <inheritdoc />
    public string LocalPlayerId { get; private set; } = string.Empty;

    /// <inheritdoc />
    public string HostPlayerId { get; private set; } = string.Empty;

    /// <inheritdoc />
    public bool IsSessionActive => _currentLobbyId.m_SteamID != 0;

    /// <summary>Creates a new Steam transport with the given codec.</summary>
    public SteamTransport(ITransportCodec codec)
    {
        _codec = codec;

        _lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(HandleLobbyCreated);
        _lobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(HandleLobbyJoinRequested);
        _lobbyEnterCallback = Callback<LobbyEnter_t>.Create(HandleLobbyEnter);
        _lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(HandleLobbyChatUpdate);
        _p2pSessionRequestCallback = Callback<P2PSessionRequest_t>.Create(HandleP2PSessionRequest);
        _p2pConnectFailCallback = Callback<P2PSessionConnectFail_t>.Create(HandleP2PConnectFail);
    }

    /// <inheritdoc />
    public Result Initialize()
    {
        Environment.SetEnvironmentVariable(SteamAppIdEnvVar, "480");

        if (!SteamAPI.Init())
            return Result.Failure("SteamAPI.Init() failed. Is Steam running?");

        LocalPlayerId = SteamUser.GetSteamID().m_SteamID.ToString();
        return Result.Success();
    }

    /// <inheritdoc />
    public void RunCallbacks()
    {
        SteamAPI.RunCallbacks();
        DrainIncomingMessages();
    }

    /// <inheritdoc />
    public Result CreateSession(string sessionName, int maxPlayers)
    {
        if (IsSessionActive)
            return Result.Failure("Already in a session.");

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, maxPlayers);
        return Result.Success();
    }

    /// <inheritdoc />
    public Result JoinSession(string sessionId)
    {
        if (IsSessionActive)
            return Result.Failure("Already in a session.");

        if (!ulong.TryParse(sessionId, out ulong lobbyId))
            return Result.Failure($"Invalid session id: {sessionId}");

        SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
        return Result.Success();
    }

    /// <inheritdoc />
    public Result LeaveSession()
    {
        if (!IsSessionActive)
            return Result.Failure("Not in a session.");

        SteamMatchmaking.LeaveLobby(_currentLobbyId);
        _currentLobbyId = default;
        HostPlayerId = string.Empty;
        SteamFriends.ClearRichPresence();
        return Result.Success();
    }

    /// <inheritdoc />
    public Result SendMessage(string type, string payload)
    {
        if (!IsSessionActive)
            return Result.Failure("Not in a session.");

        var message = new TransportMessage(LocalPlayerId, type, payload);
        string json = _codec.Serialize(message);
        byte[] data = Encoding.UTF8.GetBytes(json);

        CSteamID[] players = GetSessionPlayersInternal();
        foreach (CSteamID player in players)
        {
            SteamNetworking.SendP2PPacket(player, data, (uint)data.Length, EP2PSend.k_EP2PSendReliable, GameChannel);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public string[] GetSessionPlayers()
    {
        return GetSessionPlayersInternal().Select(id => id.m_SteamID.ToString()).ToArray();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (IsSessionActive)
        {
            SteamMatchmaking.LeaveLobby(_currentLobbyId);
            _currentLobbyId = default;
        }
        SteamAPI.Shutdown();
    }

    private CSteamID[] GetSessionPlayersInternal()
    {
        if (!IsSessionActive)
            return [];

        int count = SteamMatchmaking.GetNumLobbyMembers(_currentLobbyId);
        var members = new CSteamID[count];
        for (int i = 0; i < count; i++)
        {
            members[i] = SteamMatchmaking.GetLobbyMemberByIndex(_currentLobbyId, i);
        }
        return members;
    }

    private void DrainIncomingMessages()
    {
        byte[] buffer = new byte[ReceiveBufferSize];
        int drained = 0;

        while (drained < 256 && SteamNetworking.IsP2PPacketAvailable(out uint size, GameChannel))
        {
            if (size > 0 && size <= buffer.Length &&
                SteamNetworking.ReadP2PPacket(buffer, size, out uint bytesRead, out CSteamID remote, GameChannel) &&
                bytesRead > 0)
            {
                string json = Encoding.UTF8.GetString(buffer, 0, (int)bytesRead);
                TransportMessage message = _codec.Deserialize(json);

                var stamped = new TransportMessage(remote.m_SteamID.ToString(), message.Type, message.Payload);
                MessageReceived?.Invoke(stamped);
            }
            drained++;
        }
    }

    private void HandleLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
            return;

        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        HostPlayerId = LocalPlayerId;

        SteamMatchmaking.SetLobbyData(_currentLobbyId, LobbyDataHostSteamId, LocalPlayerId);
        SteamFriends.SetRichPresence("connect", $"steam://joinlobby/480/{_currentLobbyId.m_SteamID}");

        SessionJoined?.Invoke(_currentLobbyId.m_SteamID.ToString());
    }

    private void HandleLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SessionInviteReceived?.Invoke(callback.m_steamIDLobby.m_SteamID.ToString());
    }

    private void HandleLobbyEnter(LobbyEnter_t callback)
    {
        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        string? hostData = SteamMatchmaking.GetLobbyData(_currentLobbyId, LobbyDataHostSteamId);
        HostPlayerId = hostData ?? string.Empty;

        SteamFriends.SetRichPresence("connect", $"steam://joinlobby/480/{_currentLobbyId.m_SteamID}");

        string[] existingPlayers = GetSessionPlayers();
        foreach (string playerId in existingPlayers)
        {
            if (playerId != LocalPlayerId)
                PlayerJoined?.Invoke(playerId);
        }

        SessionJoined?.Invoke(_currentLobbyId.m_SteamID.ToString());
    }

    private void HandleLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        ulong changedId = callback.m_ulSteamIDUserChanged;
        string playerId = changedId.ToString();
        uint stateChange = callback.m_rgfChatMemberStateChange;

        const uint k_EChatMemberStateChangeEntered = 0x0001;
        const uint k_EChatMemberStateChangeLeft = 0x0002;
        const uint k_EChatMemberStateChangeDisconnected = 0x0004;

        if ((stateChange & k_EChatMemberStateChangeEntered) != 0)
        {
            if (playerId != LocalPlayerId)
                PlayerJoined?.Invoke(playerId);
        }
        else if ((stateChange & (k_EChatMemberStateChangeLeft | k_EChatMemberStateChangeDisconnected)) != 0)
        {
            PlayerLeft?.Invoke(playerId);
        }
    }

    private void HandleP2PSessionRequest(P2PSessionRequest_t callback)
    {
        SteamNetworking.AcceptP2PSessionWithUser(callback.m_steamIDRemote);
    }

    private void HandleP2PConnectFail(P2PSessionConnectFail_t callback)
    {
        Disconnected?.Invoke();
    }
}
