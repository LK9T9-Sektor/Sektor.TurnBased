using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sektor.TurnBased.UI.Core;

namespace Sektor.TurnBased.UI.ViewModels.Shared;

/// <summary>
/// Общий контрол информации о юните: показывается по правому клику на карточке
/// юнита в любой игре. Держит снимок юнита и признак открытости.
/// </summary>
public sealed partial class UnitInfoViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOpen))]
    private UnitSnapshot? unit;

    /// <summary>true — панель инфо открыта.</summary>
    public bool IsOpen => Unit is not null;

    /// <summary>Показывает снимок юнита (вызывается командой из игровой VM).</summary>
    public void Show(UnitSnapshot snapshot) => Unit = snapshot;

    [RelayCommand]
    private void Close() => Unit = null;
}
