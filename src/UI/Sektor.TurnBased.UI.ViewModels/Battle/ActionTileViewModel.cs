using CommunityToolkit.Mvvm.ComponentModel;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.UI.Core;

namespace Sektor.TurnBased.UI.ViewModels.Battle;

/// <summary>
/// Плитка действия в панели боя: квадрат с глифом и имя под ним внутри плитки.
/// Обёртка над ActionOption для выделения выбранного действия и подсказки.
/// </summary>
public sealed partial class ActionTileViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isSelected;

    /// <summary>Исходный вариант действия.</summary>
    public ActionOption Option { get; }

    /// <summary>Отображаемое имя действия.</summary>
    public string Name => Option.Name;

    /// <summary>Глиф-иконка действия (в будущем — полноценная иконка).</summary>
    public string Glyph => Option.Glyph;

    /// <summary>Описание действия для подсказки.</summary>
    public string Description => Option.Description;

    /// <summary>Человекочитаемый режим цели.</summary>
    public string TargetModeDisplay => Option.TargetMode switch
    {
        BattleTargetModes.Self => "Цель: на себя",
        BattleTargetModes.AllEnemies => "Цель: все враги",
        BattleTargetModes.SingleEnemy => "Цель: один враг",
        _ => $"Цель: {Option.TargetMode}",
    };

    public ActionTileViewModel(ActionOption option) => Option = option;
}
