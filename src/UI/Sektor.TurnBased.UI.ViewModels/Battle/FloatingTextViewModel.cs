using CommunityToolkit.Mvvm.ComponentModel;

namespace Sektor.TurnBased.UI.ViewModels.Battle;

/// <summary>
/// Всплывающий текст над карточкой юнита (урон/лечение, крит выделяется размером).
/// Значения Opacity/OffsetY обновляет BattleViewModel по таймеру — карточка просто
/// привязана к ним, анимация переживает пересоздание карточек между ходами.
/// </summary>
public sealed partial class FloatingTextViewModel : ObservableObject
{
    /// <summary>Runtime-Id актора, над карточкой которого показывается текст.</summary>
    public string TargetRuntimeId { get; }

    /// <summary>Отображаемый текст (величина урона или лечения).</summary>
    public string Text { get; }

    /// <summary>true — лечение (зелёный), false — урон (красный).</summary>
    public bool IsHeal { get; }

    /// <summary>true — критическое попадание (крупнее и золотом).</summary>
    public bool IsCrit { get; }

    [ObservableProperty]
    private double opacity = 1.0;

    [ObservableProperty]
    private double offsetY;

    public FloatingTextViewModel(string targetRuntimeId, string text, bool isHeal, bool isCrit)
    {
        TargetRuntimeId = targetRuntimeId;
        Text = text;
        IsHeal = isHeal;
        IsCrit = isCrit;
    }
}
