using Sektor.TurnBased.UI.Core;

namespace Sektor.TurnBased.UI.ViewModels.Battle;

/// <summary>
/// Представление юнита для карточки в бою: снапшот юнита, внешний вид
/// (цвет/иконка из ростера), строки ХП и флаги состояния (чей сейчас ход,
/// выбрана ли как цель, доступна ли для выбора цели).
/// </summary>
public sealed class UnitCardViewModel
{
    public UnitSnapshot Unit { get; }

    public bool IsActive { get; }

    public bool IsSelectedTarget { get; }

    public bool IsSelectable { get; }

    public bool IsDead => !Unit.IsAlive;

    /// <summary>Внешний вид юнита (иконка и цвет) из ростера.</summary>
    public UnitAppearance Appearance { get; }

    /// <summary>Ключ иконки: надгробие для погибшего, иначе — иконка юнита.</summary>
    public string IconKey => IsDead ? "IconTombstone" : Appearance.IconKey;

    /// <summary>Подпись здоровья в духе Blades: "текущее | максимум".</summary>
    public string HpLabel => Unit.VitalStat is null
        ? string.Empty
        : $"{Unit.VitalStat.Current} | {Unit.VitalStat.Max}";

    /// <summary>Цвет полосы ХП: красный при критическом состоянии, серый — мёртв.</summary>
    public string HpColorHex
    {
        get
        {
            if (IsDead)
                return "#555555";
            var vital = Unit.VitalStat;
            if (vital is not null && vital.Max > 0 && vital.Current <= vital.Max * 0.25)
                return "#E53935";
            return "#4CAF50";
        }
    }

    public UnitCardViewModel(UnitSnapshot unit, bool isActive, bool isSelectedTarget, bool isSelectable)
    {
        Unit = unit;
        IsActive = isActive;
        IsSelectedTarget = isSelectedTarget;
        IsSelectable = isSelectable;
        Appearance = UnitAppearances.ForTemplate(unit.TemplateId, unit.TeamId);
    }
}
