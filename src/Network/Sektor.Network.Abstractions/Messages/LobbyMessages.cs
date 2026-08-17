using Sektor.Network.Abstractions.Lobby;

namespace Sektor.Network.Abstractions.Messages;

/// <summary>Типы сообщений лобби-протокола (игронезависимые).</summary>
public static class LobbyMessageTypes
{
    public const string LobbyUpdate = "lobby_update";
    public const string PlayerReady = "player_ready";
}

/// <summary>Хост рассылает полное состояние лобби.</summary>
public sealed record LobbyUpdateMessage(
    string HostId,
    int MaxPlayers,
    IReadOnlyList<LobbyPlayer> Players);

/// <summary>Клиент сообщает о готовности.</summary>
public sealed record PlayerReadyMessage(bool IsReady);