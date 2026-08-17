using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sektor.Network.Abstractions.Lobby;
using Sektor.TurnBased.UI.Core;
using Sektor.TurnBased.UI.Core.Multiplayer;
using Sektor.TurnBased.UI.ViewModels.Navigation;
using Sektor.TurnBased.UI.ViewModels.Shared;

namespace Sektor.TurnBased.UI.ViewModels.Multiplayer;

/// <summary>
/// VM экрана мультиплеер-лобби. Управляет подключением, выбором класса и готовностью,
/// а после старта строит детерминированный бой (по seed и назначениям) и переходит на него.
/// Транспорт/Steam наружу не протекает — только игровой слой BattleLobbySession.
/// </summary>
public sealed partial class MultiplayerLobbyViewModel : ObservableObject, IUpdatable
{
    private readonly NavigationManager _navigation;
    private readonly BattleLobbySession _session;
    private readonly Func<GameSession, IGameViewModel> _gameViewModelFactory;
    private Action<string>? _setClipboard;

    [ObservableProperty]
    private string status = "Создайте лобби или присоединитесь к существующему.";

    [ObservableProperty]
    private string lobbyId = string.Empty;

    [ObservableProperty]
    private string joinSessionId = string.Empty;

    [ObservableProperty]
    private bool isSessionActive;

    [ObservableProperty]
    private bool isHost;

    [ObservableProperty]
    private bool canStart;

    [ObservableProperty]
    private bool isReady;

    [ObservableProperty]
    private string currentClassName = "Выберите класс";

    [ObservableProperty]
    private string currentClassDescription = string.Empty;

    public ObservableCollection<PlayerInfoViewModel> Players { get; } = [];

    /// <summary>Создаёт VM лобби. gameViewModelFactory — фабрика игровой VM по сессии.</summary>
    public MultiplayerLobbyViewModel(
        NavigationManager navigation,
        BattleLobbySession session,
        Func<GameSession, IGameViewModel> gameViewModelFactory)
    {
        _navigation = navigation;
        _session = session;
        _gameViewModelFactory = gameViewModelFactory;
        _session.GameStarted += OnGameStarted;
    }

    /// <summary>Клиент получил start_game: строим бой и переходим на экран боя.</summary>
    private void OnGameStarted() => StartMultiplayerBattle();

    /// <summary>Зарегистрировать callback для копирования в буфер (вызывать из View).</summary>
    public void SetClipboardCallback(Action<string> callback)
    {
        _setClipboard = callback;
    }

    /// <summary>Инициализировать транспорт.</summary>
    [RelayCommand]
    private void Initialize()
    {
        var result = _session.Coordinator.Initialize();
        Status = result.IsSuccess
            ? $"Steam инициализирован. Ваш ID: {_session.Coordinator.LocalPlayerId}"
            : $"Ошибка: {result.Error}";
    }

    /// <summary>Создать лобби (хост).</summary>
    [RelayCommand]
    private void CreateLobby()
    {
        var result = _session.Coordinator.CreateLobby("Хост", 4);
        if (result.IsFailure)
        {
            Status = $"Ошибка: {result.Error}";
            return;
        }

        LobbyId = _session.Coordinator.LocalPlayerId;
        RefreshState();
        Status = $"Лобби создано. ID: {LobbyId}. Ожидание игроков...";
    }

    /// <summary>Присоединиться к лобби.</summary>
    [RelayCommand]
    private void JoinLobby()
    {
        if (string.IsNullOrWhiteSpace(JoinSessionId))
        {
            Status = "Введите Lobby ID.";
            return;
        }

        var result = _session.Coordinator.JoinLobby(JoinSessionId.Trim(), "Клиент");
        if (result.IsFailure)
        {
            Status = $"Ошибка: {result.Error}";
            return;
        }

        RefreshState();
        Status = "Подключено. Выберите класс и нажмите «Готов».";
    }

    /// <summary>Скопировать Lobby ID в буфер.</summary>
    [RelayCommand]
    private void CopyLobbyId()
    {
        if (!string.IsNullOrEmpty(LobbyId))
            _setClipboard?.Invoke(LobbyId);
    }

    /// <summary>Выбрать класс влево.</summary>
    [RelayCommand]
    private void SelectClassLeft()
    {
        _session.SelectClassLeft();
        RefreshClassInfo();
    }

    /// <summary>Выбрать класс вправо.</summary>
    [RelayCommand]
    private void SelectClassRight()
    {
        _session.SelectClassRight();
        RefreshClassInfo();
    }

    /// <summary>Переключить готовность.</summary>
    [RelayCommand]
    private void ToggleReady()
    {
        _session.ToggleReady();
        IsReady = _session.Coordinator.LocalReady;
    }

    /// <summary>Хост начинает игру.</summary>
    [RelayCommand(CanExecute = nameof(CanStart))]
    private void StartGame()
    {
        var result = _session.StartGame();
        if (result.IsFailure)
        {
            Status = $"Ошибка: {result.Error}";
            return;
        }

        Status = "Игра начинается!";
        StartMultiplayerBattle();
    }

    /// <summary>
    /// Строит сетевой бой по seed и назначениям (одинаковый на всех клиентах),
    /// переходит на его экран и запускает. Локальный слот — позиция локального игрока
    /// в назначениях (для фильтра доступных действий).
    /// </summary>
    private void StartMultiplayerBattle()
    {
        if (_session.Seed is not { } seed || _session.Assignments is null)
        {
            Status = "Нет данных для старта боя.";
            return;
        }

        var localSlot = LocalSlot();
        var created = GameSessionFactory.CreateMultiplayerBattle(
            seed, _session.Assignments, _session.BuildPresentations(), _session.Coordinator, localSlot);
        if (created.IsFailure)
        {
            Status = $"Ошибка: {created.Error}";
            return;
        }

        var game = _gameViewModelFactory(created.Value!);
        _navigation.NavigateTo(game);
        _ = game.RunAsync();
    }

    private int? LocalSlot()
    {
        if (_session.Assignments is not { } assignments)
            return null;
        var localId = _session.Coordinator.LocalPlayerId;
        for (int i = 0; i < assignments.Count; i++)
        {
            if (assignments[i].PlayerId == localId)
                return i;
        }
        return null;
    }

    /// <summary>Вернуться в одиночное лобби.</summary>
    [RelayCommand]
    private void GoToLobby()
    {
        _navigation.NavigateTo(Pages.Lobby);
    }

    /// <summary>Обновить состояние (вызывать по таймеру или в цикле).</summary>
    public void Update()
    {
        _session.Coordinator.Update();
        RefreshState();
    }

    private void RefreshState()
    {
        IsSessionActive = _session.Coordinator.IsSessionActive;
        IsHost = _session.Coordinator.IsHost;
        LobbyId = _session.Coordinator.Snapshot?.HostId ?? string.Empty;
        CanStart = _session.Coordinator.IsHost && _session.Coordinator.Snapshot is { AllReady: true };
        IsReady = _session.Coordinator.LocalReady;

        RefreshPlayers();
        RefreshClassInfo();
        StartGameCommand.NotifyCanExecuteChanged();
    }

    private void RefreshPlayers()
    {
        var snapshot = _session.Coordinator.Snapshot;
        if (snapshot is null) return;

        while (Players.Count > snapshot.Players.Count)
            Players.RemoveAt(Players.Count - 1);

        for (int i = 0; i < snapshot.Players.Count; i++)
        {
            var info = snapshot.Players[i];
            string color = PlayerColors.Get(i);
            string className = _session.ClassDisplayName(_session.PlayerClassId(info.PlayerId));
            if (i < Players.Count)
            {
                Players[i].Apply(info, info.PlayerId == snapshot.HostId, color, className);
            }
            else
            {
                var vm = new PlayerInfoViewModel();
                vm.Apply(info, info.PlayerId == snapshot.HostId, color, className);
                Players.Add(vm);
            }
        }
    }

    private void RefreshClassInfo()
    {
        string classId = _session.LocalClassId;
        CurrentClassName = string.IsNullOrEmpty(classId) ? "Выберите класс" : _session.ClassDisplayName(classId);
        CurrentClassDescription = _session.ClassDescription(classId);
    }
}