using Sektor.TurnBased.UI.Core;

namespace Sektor.TurnBased.UI.ViewModels.Battle;

/// <summary>
/// Элемент очереди ходов: юнит в порядке хода текущего раунда с внешним видом
/// (цвет/иконка), скоростью и флагами — сейчас ходит / уже сходил. Иммутабелен,
/// пересоздаётся при обновлении снапшота.
/// </summary>
public sealed class TurnOrderItemViewModel
{
    public string DisplayName { get; }

    public UnitAppearance Appearance { get; }

    public int Speed { get; }

    public bool IsActive { get; }

    public bool HasActed { get; }

    public TurnOrderItemViewModel(UnitSnapshot unit, bool isActive, bool hasActed)
    {
        DisplayName = unit.DisplayName;
        Appearance = UnitAppearances.ForTemplate(unit.TemplateId, unit.TeamId);
        Speed = unit.Stats.FirstOrDefault(s => s.StatId == "initiative")?.Current ?? 0;
        IsActive = isActive;
        HasActed = hasActed;
    }
}
