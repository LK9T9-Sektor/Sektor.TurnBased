using CommunityToolkit.Mvvm.ComponentModel;
using Sektor.Network.Abstractions.Lobby;

namespace Sektor.TurnBased.UI.ViewModels.Multiplayer;

/// <summary>
/// VM одного игрока в списке лобби.
/// </summary>
public sealed partial class PlayerInfoViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string color = string.Empty;

    [ObservableProperty]
    private bool isReady;

    [ObservableProperty]
    private string className = string.Empty;

    [ObservableProperty]
    private bool isHost;

    /// <summary>Обновить из данных лобби.</summary>
    public void Apply(LobbyPlayer info, bool isHostPlayer, string color, string className)
    {
        Name = info.Name;
        Color = color;
        IsReady = info.IsReady;
        ClassName = className;
        IsHost = isHostPlayer;
    }
}