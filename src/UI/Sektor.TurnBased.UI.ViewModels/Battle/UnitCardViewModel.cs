using Sektor.TurnBased.UI.Core;

namespace Sektor.TurnBased.UI.ViewModels.Battle;

/// <summary>
/// Представление юнита для карточки в бою: снапшот юнита плюс флаги состояния
/// (чей сейчас ход, выбрана ли как цель, доступна ли для выбора цели).
/// </summary>
public sealed class UnitCardViewModel
{
    public UnitSnapshot Unit { get; }

    public bool IsActive { get; }

    public bool IsSelectedTarget { get; }

    public bool IsSelectable { get; }

    public bool IsDead => !Unit.IsAlive;

    public UnitCardViewModel(UnitSnapshot unit, bool isActive, bool isSelectedTarget, bool isSelectable)
    {
        Unit = unit;
        IsActive = isActive;
        IsSelectedTarget = isSelectedTarget;
        IsSelectable = isSelectable;
    }
}
