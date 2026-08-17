namespace Sektor.Network.Abstractions.Lobby;

/// <summary>
/// Данные одного игрока в лобби. PlayerId — непрозрачный идентификатор,
/// назначенный транспортом; без привязки к конкретному провайдеру.
/// </summary>
public sealed record LobbyPlayer(
    string PlayerId,
    string Name,
    bool IsReady);