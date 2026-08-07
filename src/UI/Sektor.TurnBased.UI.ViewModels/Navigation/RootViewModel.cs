using CommunityToolkit.Mvvm.ComponentModel;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.UI.Core;
using Sektor.TurnBased.UI.ViewModels.Battle;
using Sektor.TurnBased.UI.ViewModels.Dialog;
using Sektor.TurnBased.UI.ViewModels.Lobby;
using Sektor.TurnBased.UI.ViewModels.Shared;

namespace Sektor.TurnBased.UI.ViewModels.Navigation;

/// <summary>
/// Корневая VM окна: держит навигацию, общие контролы (инфо о юните, подтверждение)
/// и лобби. Игровые VM создаёт по фабрике видов — словарь по виду игры, без switch.
/// </summary>
public sealed class RootViewModel : ObservableObject
{
    private readonly IReadOnlyDictionary<string, Func<GameSession, IGameViewModel>> _viewModels;

    /// <summary>Навигация между экранами.</summary>
    public NavigationManager Navigation { get; }

    /// <summary>Общий контрол информации о юните (правый клик).</summary>
    public UnitInfoViewModel UnitInfo { get; }

    /// <summary>Общий контрол подтверждения (конец хода/действие).</summary>
    public ConfirmationViewModel Confirmation { get; }

    /// <summary>Общие настройки UX (подтверждение хода, пульсация, виньетка).</summary>
    public SettingsViewModel Settings { get; }

    /// <summary>Лобби (первая страница).</summary>
    public LobbyViewModel Lobby { get; }

    public RootViewModel(
        NavigationManager navigation,
        UnitInfoViewModel unitInfo,
        ConfirmationViewModel confirmation,
        SettingsViewModel settings,
        Func<string, int, Result<GameSession>> sessionFactory)
    {
        Navigation = navigation;
        UnitInfo = unitInfo;
        Confirmation = confirmation;
        Settings = settings;

        _viewModels = new Dictionary<string, Func<GameSession, IGameViewModel>>
        {
            [GameKinds.Battle] = session => new BattleViewModel((BattleSession)session, navigation, unitInfo, confirmation, settings),
            [GameKinds.BattleLine] = session => new BattleViewModel((BattleSession)session, navigation, unitInfo, confirmation, settings),
            [GameKinds.Dialog] = session => new DialogViewModel((DialogSession)session, navigation),
        };

        Lobby = new LobbyViewModel(navigation, sessionFactory, CreateGameViewModel);
        Navigation.Register(Pages.Lobby, Lobby);
        Navigation.NavigateTo(Pages.Lobby);
    }

    private IGameViewModel CreateGameViewModel(GameSession session) =>
        _viewModels.TryGetValue(session.Kind, out var factory)
            ? factory(session)
            : throw new InvalidOperationException($"No view model factory for game '{session.Kind}'.");
}
