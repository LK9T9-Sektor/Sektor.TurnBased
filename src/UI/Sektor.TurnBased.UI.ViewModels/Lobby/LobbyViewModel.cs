using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.UI.Core;
using Sektor.TurnBased.UI.ViewModels.Navigation;
using Sektor.TurnBased.UI.ViewModels.Shared;

namespace Sektor.TurnBased.UI.ViewModels.Lobby;

/// <summary>
/// Лобби: выбор игры и seed, запуск сессии и переход к игровой VM. Кооп — заглушка
/// (сеть отдельным этапом). Запуск через инжектированные фабрики, без событий.
/// </summary>
public sealed partial class LobbyViewModel : ObservableObject
{
    private readonly NavigationManager _navigation;
    private readonly Func<string, int, Result<GameSession>> _sessionFactory;
    private readonly Func<GameSession, IGameViewModel> _viewModelFactory;

    [ObservableProperty]
    private string selectedGame = GameKinds.Battle;

    [ObservableProperty]
    private string seedText = "42";

    [ObservableProperty]
    private string? error;

    /// <summary>Доступные игры (заголовки лобби).</summary>
    public IReadOnlyList<string> Games { get; } = GameKinds.All;

    /// <summary>Кооп-режим ещё не реализован (сеть отдельным этапом).</summary>
    public bool IsCoopAvailable => false;

    public LobbyViewModel(
        NavigationManager navigation,
        Func<string, int, Result<GameSession>> sessionFactory,
        Func<GameSession, IGameViewModel> viewModelFactory)
    {
        _navigation = navigation;
        _sessionFactory = sessionFactory;
        _viewModelFactory = viewModelFactory;
    }

    [RelayCommand]
    private void Start()
    {
        var created = _sessionFactory(SelectedGame, ParseSeed());
        if (created.IsFailure)
        {
            Error = created.Error;
            return;
        }

        Error = null;
        var gameViewModel = _viewModelFactory(created.Value!);
        _navigation.NavigateTo(gameViewModel);
        _ = gameViewModel.RunAsync();
    }

    private int ParseSeed()
    {
        if (int.TryParse(SeedText, out var seed))
            return seed;
        return string.IsNullOrEmpty(SeedText) ? 42 : StableHash(SeedText);
    }

    private static int StableHash(string text)
    {
        var hash = 17;
        foreach (var ch in text)
            hash = unchecked(hash * 31 + ch);
        return hash;
    }
}
