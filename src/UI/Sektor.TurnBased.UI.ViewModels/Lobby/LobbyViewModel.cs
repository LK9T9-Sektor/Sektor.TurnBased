using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.UI.Core;
using Sektor.TurnBased.UI.ViewModels.Navigation;
using Sektor.TurnBased.UI.ViewModels.Shared;

namespace Sektor.TurnBased.UI.ViewModels.Lobby;

/// <summary>
/// Лобби: выбор режима боя (два ряда / одна линия) или диалога карточками
/// вместо выпадающего списка, ввод seed и запуск сессии. Запуск через
/// инжектированные фабрики, без событий.
/// </summary>
public sealed partial class LobbyViewModel : ObservableObject
{
    private readonly NavigationManager _navigation;
    private readonly Func<string, int, Result<GameSession>> _sessionFactory;
    private readonly Func<GameSession, IGameViewModel> _viewModelFactory;

    [ObservableProperty]
    private LobbyGameOption? selectedOption;

    [ObservableProperty]
    private string seedText = "42";

    [ObservableProperty]
    private string? error;

    /// <summary>Доступные варианты игр (карточки лобби).</summary>
    public ObservableCollection<LobbyGameOption> Options { get; } = new();

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

        Options.Add(new LobbyGameOption(
            GameKinds.Battle,
            "Бой · Два ряда",
            "Информативные карточки юнитов: две линии, статы и статусы внутри"));
        Options.Add(new LobbyGameOption(
            GameKinds.BattleLine,
            "Бой · Одна линия",
            "Минимализм в духе Blades: иконка, имя и полоса ХП снизу"));
        Options.Add(new LobbyGameOption(
            GameKinds.Dialog,
            "Диалог",
            "Ветвящийся квест с вариантами ответов и флагами"));

        SelectedOption = Options[0];
        SelectedOption.IsSelected = true;
    }

    partial void OnSelectedOptionChanged(LobbyGameOption? value)
    {
        foreach (var option in Options)
            option.IsSelected = ReferenceEquals(option, value);
    }

    [RelayCommand]
    private void SelectOption(LobbyGameOption? option)
    {
        if (option is not null)
            SelectedOption = option;
    }

    [RelayCommand]
    private void Start()
    {
        if (SelectedOption is not { } option)
        {
            Error = "Выберите игру.";
            return;
        }

        var created = _sessionFactory(option.Kind, ParseSeed());
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
