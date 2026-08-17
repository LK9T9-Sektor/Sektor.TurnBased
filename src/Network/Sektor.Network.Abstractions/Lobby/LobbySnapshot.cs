namespace Sektor.Network.Abstractions.Lobby;

/// <summary>
/// Снимок состояния лобби. Immutable.
/// </summary>
public sealed class LobbySnapshot
{
    /// <summary>ID хоста.</summary>
    public string HostId { get; }

    /// <summary>Максимальное число игроков.</summary>
    public int MaxPlayers { get; }

    /// <summary>Список игроков (включая хоста).</summary>
    public IReadOnlyList<LobbyPlayer> Players { get; }

    /// <summary>Все ли готовы (хост + минимум 1 клиент).</summary>
    public bool AllReady => Players.Count >= 2 && Players.All(p => p.IsReady);

    /// <summary>Заполнено ли лобби.</summary>
    public bool IsFull => Players.Count >= MaxPlayers;

    /// <summary>Создаёт снимок лобби.</summary>
    public LobbySnapshot(string hostId, int maxPlayers, IReadOnlyList<LobbyPlayer> players)
    {
        HostId = hostId;
        MaxPlayers = maxPlayers;
        Players = players;
    }
}