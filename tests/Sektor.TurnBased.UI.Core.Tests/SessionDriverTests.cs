using Sektor.TurnBased.UI.Core;
using Xunit;

namespace Sektor.TurnBased.UI.Core.Tests;

/// <summary>
/// Тесты SessionDriver и GameSessionFactory: продвижение сессии, создание игр
/// по идентификатору и обработка неизвестной игры.
/// </summary>
public class SessionDriverTests
{
    [Fact]
    public void Step_OnStartedDialog_ReturnsSuccessAndStaysAwaitingInput()
    {
        var (_, _, session) = TestHelpers.CreateDialog(seed: 7);
        Assert.True(session.Start().IsSuccess);
        Assert.True(session.NeedsInput);

        var stepped = SessionDriver.Step(session);

        Assert.True(stepped.IsSuccess, stepped.Error);
        Assert.True(session.NeedsInput);
        Assert.False(session.IsFinished);
    }

    [Fact]
    public void Factory_CreatesBattleAndDialogSessions()
    {
        var battle = GameSessionFactory.Create(GameKinds.Battle, seed: 42);
        var dialog = GameSessionFactory.Create(GameKinds.Dialog, seed: 7);

        Assert.True(battle.IsSuccess, battle.Error);
        Assert.True(dialog.IsSuccess, dialog.Error);
        Assert.Equal(GameKinds.Battle, battle.Value!.Kind);
        Assert.Equal(GameKinds.Dialog, dialog.Value!.Kind);
    }

    [Fact]
    public void Factory_UnknownGame_ReturnsFailure()
    {
        var result = GameSessionFactory.Create("unknown", seed: 1);

        Assert.True(result.IsFailure);
        Assert.Contains("unknown", result.Error);
    }
}
