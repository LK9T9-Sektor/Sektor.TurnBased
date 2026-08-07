using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sektor.TurnBased.UI.ViewModels.Battle;

namespace Sektor.TurnBased.UI.ViewModels.Shared;

/// <summary>
/// Общий контрол подсказки о способности: показывается по правому клику на плитке
/// действия в любой игре. Держит плитку действия и признак открытости.
/// </summary>
public sealed partial class AbilityInfoViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOpen))]
    private ActionTileViewModel? tile;

    /// <summary>true — панель подсказки открыта.</summary>
    public bool IsOpen => Tile is not null;

    /// <summary>Показывает плитку действия (вызывается командой из игровой VM).</summary>
    public void Show(ActionTileViewModel tile) => Tile = tile;

    [RelayCommand]
    private void Close() => Tile = null;
}
