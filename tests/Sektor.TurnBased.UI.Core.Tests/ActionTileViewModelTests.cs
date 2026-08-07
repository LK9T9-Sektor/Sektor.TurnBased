using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.UI.Core;
using Sektor.TurnBased.UI.ViewModels.Battle;
using Xunit;

namespace Sektor.TurnBased.UI.Core.Tests;

/// <summary>
/// Тесты плитки действия: обёртка ActionOption с глифом, описанием,
/// человекочитаемой целью и выделением выбранного действия.
/// </summary>
public class ActionTileViewModelTests
{
    [Fact]
    public void ExposesNameGlyphAndDescription_FromOption()
    {
        var tile = new ActionTileViewModel(new ActionOption(
            "basic_attack", "Удар", BattleTargetModes.SingleEnemy, "⚔", "Описание удара."));

        Assert.Equal("Удар", tile.Name);
        Assert.Equal("⚔", tile.Glyph);
        Assert.Equal("Описание удара.", tile.Description);
        Assert.Equal("Цель: один враг", tile.TargetModeDisplay);
        Assert.False(tile.IsSelected);
    }

    [Theory]
    [InlineData(BattleTargetModes.Self, "Цель: на себя")]
    [InlineData(BattleTargetModes.SingleEnemy, "Цель: один враг")]
    [InlineData(BattleTargetModes.AllEnemies, "Цель: все враги")]
    public void TargetModeDisplay_MapsKnownModes(string mode, string expected)
    {
        var tile = new ActionTileViewModel(new ActionOption("a", "A", mode));

        Assert.Equal(expected, tile.TargetModeDisplay);
    }

    [Fact]
    public void IsSelected_RaisesPropertyChanged()
    {
        var tile = new ActionTileViewModel(new ActionOption("a", "A", BattleTargetModes.Self));
        var raised = false;
        tile.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ActionTileViewModel.IsSelected))
                raised = true;
        };

        tile.IsSelected = true;

        Assert.True(raised);
    }
}
