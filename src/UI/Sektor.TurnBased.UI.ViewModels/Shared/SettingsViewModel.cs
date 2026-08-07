using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sektor.TurnBased.UI.ViewModels.Shared;

/// <summary>
/// Настройки игрока: переключатели UX-эффектов (подтверждение конца хода,
/// пульсация активного юнита, кровавая виньетка) и признак открытой панели.
/// Общий контрол для всех игр; состояние живёт в памяти сессии.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    /// <summary>true — перед завершением хода спрашивать подтверждение.</summary>
    [ObservableProperty]
    private bool confirmEndTurn = true;

    /// <summary>true — активный юнит пульсирует (анимация масштаба).</summary>
    [ObservableProperty]
    private bool pulseEffects = true;

    /// <summary>true — на поле боя показывается кровавая виньетка по краям.</summary>
    [ObservableProperty]
    private bool redVignette = true;

    /// <summary>true — панель настроек открыта.</summary>
    [ObservableProperty]
    private bool isOpen;

    /// <summary>Открывает/закрывает панель настроек (кнопка-глиф в шапке окна).</summary>
    [RelayCommand]
    private void Toggle() => IsOpen = !IsOpen;
}
