using System.Text.Json;
using Sektor.Network.Abstractions.Messages;
using Sektor.Network.Abstractions.Transport;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.Network.Abstractions.Lobby;

/// <summary>
/// Игронезависимый координатор лобби поверх ITransport. Polling-модель:
/// вызывающий слой дергает Update() каждый кадр. Протокольные сообщения
/// (lobby_update, player_ready) обрабатываются внутри; все остальные типы
/// пересылаются в GameMessageReceived для игрового слоя.
/// </summary>
public sealed class LobbyCoordinator
{
    private readonly ITransport _transport;
    private readonly List<LobbyPlayer> _players = [];
    private LobbySnapshot? _snapshot;
    private bool _isHost;
    private string _localName = string.Empty;
    private bool _localReady;

    /// <summary>Событие игровых сообщений (типы вне лобби-протокола).</summary>
    public event Action<TransportMessage>? GameMessageReceived;

    /// <summary>Текущий снимок лобби (null до создания/входа).</summary>
    public LobbySnapshot? Snapshot => _snapshot;

    /// <summary>Я ли хост.</summary>
    public bool IsHost => _isHost;

    /// <summary>Готов ли локальный игрок.</summary>
    public bool LocalReady => _localReady;

    /// <summary>ID локального игрока.</summary>
    public string LocalPlayerId => _transport.LocalPlayerId;

    /// <summary>Активна ли сессия.</summary>
    public bool IsSessionActive => _transport.IsSessionActive;

    /// <summary>Создаёт координатор лобби.</summary>
    public LobbyCoordinator(ITransport transport)
    {
        _transport = transport;

        _transport.SessionJoined += OnSessionJoined;
        _transport.PlayerJoined += OnPlayerJoined;
        _transport.PlayerLeft += OnPlayerLeft;
        _transport.MessageReceived += OnMessageReceived;
    }

    /// <summary>Инициализирует транспорт.</summary>
    public Result Initialize()
    {
        return _transport.Initialize();
    }

    /// <summary>Хост создаёт лобби.</summary>
    public Result CreateLobby(string hostName, int maxPlayers = 4)
    {
        _localName = hostName;
        _isHost = true;

        var result = _transport.CreateSession("lobby", maxPlayers);
        if (result.IsFailure)
            return result;

        _localReady = true;

        _players.Clear();
        _players.Add(new LobbyPlayer(_transport.LocalPlayerId, hostName, true));

        _snapshot = new LobbySnapshot(_transport.LocalPlayerId, maxPlayers, [.. _players]);
        BroadcastLobbyUpdate();
        return Result.Success();
    }

    /// <summary>Клиент присоединяется к лобби по ID.</summary>
    public Result JoinLobby(string sessionId, string playerName)
    {
        _localName = playerName;
        _isHost = false;

        var result = _transport.JoinSession(sessionId);
        if (result.IsFailure)
            return result;

        _localReady = false;
        return Result.Success();
    }

    /// <summary>Покинуть текущее лобби.</summary>
    public Result LeaveLobby()
    {
        var result = _transport.LeaveSession();
        if (result.IsFailure)
            return result;

        _players.Clear();
        _snapshot = null;
        _isHost = false;
        _localReady = false;
        return Result.Success();
    }

    /// <summary>Переключить готовность (только не-хост).</summary>
    public Result ToggleReady()
    {
        if (_isHost)
            return Result.Failure("Хост не отмечает готовность — он всегда готов.");

        _localReady = !_localReady;

        var idx = _players.FindIndex(p => p.PlayerId == _transport.LocalPlayerId);
        if (idx >= 0)
        {
            var old = _players[idx];
            _players[idx] = old with { IsReady = _localReady };
            RebuildSnapshot();
        }

        SendToHost(LobbyMessageTypes.PlayerReady,
            JsonSerializer.Serialize(new PlayerReadyMessage(_localReady)));
        return Result.Success();
    }

    /// <summary>Отправить игровое сообщение хосту.</summary>
    public Result SendToHost(string type, string payload)
    {
        return _transport.SendMessage(type, payload);
    }

    /// <summary>Отправить игровое сообщение всем удалённым участникам.</summary>
    public Result SendToAll(string type, string payload)
    {
        string[] players = _transport.GetSessionPlayers();
        foreach (string _ in players)
        {
            var result = _transport.SendMessage(type, payload);
            if (result.IsFailure)
                return result;
        }
        return Result.Success();
    }

    /// <summary>Прогнать колбэки транспорта (вызывать каждый кадр).</summary>
    public void Update()
    {
        _transport.RunCallbacks();
    }

    private void OnSessionJoined(string sessionId)
    {
    }

    private void OnPlayerJoined(string playerId)
    {
        if (!_isHost) return;

        var playerIndex = _players.Count;
        var playerName = $"Игрок {playerIndex + 1}";
        _players.Add(new LobbyPlayer(playerId, playerName, false));
        RebuildSnapshot();
        BroadcastLobbyUpdate();
    }

    private void OnPlayerLeft(string playerId)
    {
        if (!_isHost) return;

        _players.RemoveAll(p => p.PlayerId == playerId);
        RebuildSnapshot();
        BroadcastLobbyUpdate();
    }

    private void OnMessageReceived(TransportMessage message)
    {
        if (message.SenderId == _transport.LocalPlayerId) return;

        switch (message.Type)
        {
            case LobbyMessageTypes.LobbyUpdate:
                HandleLobbyUpdate(message.Payload);
                break;
            case LobbyMessageTypes.PlayerReady:
                HandlePlayerReady(message.SenderId, message.Payload);
                break;
            default:
                GameMessageReceived?.Invoke(message);
                break;
        }
    }

    private void HandleLobbyUpdate(string payload)
    {
        var msg = JsonSerializer.Deserialize<LobbyUpdateMessage>(payload);
        if (msg is null) return;

        _players.Clear();
        foreach (var p in msg.Players)
            _players.Add(new LobbyPlayer(p.PlayerId, p.Name, p.IsReady));

        _snapshot = new LobbySnapshot(msg.HostId, msg.MaxPlayers, [.. _players]);
    }

    private void HandlePlayerReady(string senderId, string payload)
    {
        var msg = JsonSerializer.Deserialize<PlayerReadyMessage>(payload);
        if (msg is null) return;

        var idx = _players.FindIndex(p => p.PlayerId == senderId);
        if (idx < 0) return;

        var old = _players[idx];
        _players[idx] = old with { IsReady = msg.IsReady };
        RebuildSnapshot();
    }

    private void BroadcastLobbyUpdate()
    {
        var payload = JsonSerializer.Serialize(new LobbyUpdateMessage(
            _transport.LocalPlayerId, _snapshot?.MaxPlayers ?? 4, [.. _players]));
        SendToAll(LobbyMessageTypes.LobbyUpdate, payload);
    }

    private void RebuildSnapshot()
    {
        string hostId = _isHost ? _transport.LocalPlayerId : _snapshot?.HostId ?? string.Empty;
        int maxPlayers = _snapshot?.MaxPlayers ?? 4;
        _snapshot = new LobbySnapshot(hostId, maxPlayers, [.. _players]);
    }
}