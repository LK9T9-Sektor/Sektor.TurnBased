using System.Text.Json;
using Sektor.Network.Abstractions.Lobby;
using Sektor.Network.Abstractions.Transport;
using Sektor.TurnBased.Battle.Content;
using Sektor.TurnBased.Core.Abstractions;

namespace Sektor.TurnBased.UI.Core.Multiplayer;

/// <summary>
/// Игровой слой лобби поверх LobbyCoordinator: каталог героев боя, выбор класса
/// и назначения при старте. Лобби-протокол про классы не знает — это игровые данные.
/// </summary>
public sealed class BattleLobbySession
{
    private readonly LobbyCoordinator _coordinator;
    private readonly string[] _heroIds;
    private readonly Dictionary<string, string> _classByPlayerId = [];
    private IReadOnlyList<PlayerAssignment>? _assignments;
    private string _localClassId = string.Empty;
    private int? _seed;

    /// <summary>Создаёт игровой слой лобби с каталогом героев боя по умолчанию.</summary>
    public BattleLobbySession(LobbyCoordinator coordinator)
        : this(coordinator, BattleContentCatalog.PlayerHeroIds)
    {
    }

    /// <summary>Создаёт игровой слой лобби с явным каталогом героев.</summary>
    public BattleLobbySession(LobbyCoordinator coordinator, IReadOnlyList<string> heroIds)
    {
        _coordinator = coordinator;
        _heroIds = heroIds.ToArray();
        _coordinator.GameMessageReceived += OnGameMessage;
    }

    /// <summary>Координатор лобби.</summary>
    public LobbyCoordinator Coordinator => _coordinator;

    /// <summary>Доступные для выбора герои.</summary>
    public IReadOnlyList<string> HeroIds => _heroIds;

    /// <summary>Текущий класс локального игрока (пусто до выбора).</summary>
    public string LocalClassId => _localClassId;

    /// <summary>Назначения после StartGame (null до старта).</summary>
    public IReadOnlyList<PlayerAssignment>? Assignments => _assignments;

    /// <summary>Seed боя (null до старта).</summary>
    public int? Seed => _seed;

    /// <summary>Событие: получено start_game (клиенты строят бой и переходят на экран).</summary>
    public event Action? GameStarted;

    /// <summary>Отображаемое имя класса (для выбора и списка игроков).</summary>
    public string ClassDisplayName(string? classId) => classId switch
    {
        "hero_warrior" => "Воин",
        "hero_rogue" => "Разбойник",
        "hero_archer" => "Лучник",
        "hero_priestess" => "Жрица",
        _ => "—"
    };

    /// <summary>Описание класса (для выбора).</summary>
    public string ClassDescription(string? classId) => classId switch
    {
        "hero_warrior" => "HP 100 · ATK 12 · Armor 3\nУдар, Мощный удар, Боевой клич, Исцеление",
        "hero_rogue" => "HP 80 · ATK 15 · Armor 1\nУдар, Мощный удар",
        "hero_archer" => "HP 70 · ATK 10 · Armor 1\nУдар, Мощный удар",
        "hero_priestess" => "HP 90 · ATK 5 · Armor 2\nУдар, Исцеление",
        _ => string.Empty
    };

    /// <summary>Класс игрока по PlayerId (выбранный либо фолбэк по каталогу).</summary>
    public string? PlayerClassId(string playerId)
    {
        if (_classByPlayerId.TryGetValue(playerId, out var selected))
            return selected;

        var players = _coordinator.Snapshot?.Players ?? [];
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].PlayerId == playerId)
                return _heroIds[i % _heroIds.Length];
        }
        return null;
    }

    /// <summary>Выбрать класс (влево).</summary>
    public Result SelectClassLeft()
    {
        var idx = Array.IndexOf(_heroIds, _localClassId);
        idx = idx <= 0 ? _heroIds.Length - 1 : idx - 1;
        return SelectClass(_heroIds[idx]);
    }

    /// <summary>Выбрать класс (вправо).</summary>
    public Result SelectClassRight()
    {
        var idx = Array.IndexOf(_heroIds, _localClassId);
        idx = idx >= _heroIds.Length - 1 ? 0 : idx + 1;
        return SelectClass(_heroIds[idx]);
    }

    /// <summary>Переключить готовность локального игрока.</summary>
    public Result ToggleReady()
    {
        return _coordinator.ToggleReady();
    }

    /// <summary>Хост запускает игру: строит назначения и рассылает их с seed боя.</summary>
    public Result StartGame()
    {
        if (!_coordinator.IsHost)
            return Result.Failure("Только хост может начать игру.");

        if (_coordinator.Snapshot is null || !_coordinator.Snapshot.AllReady)
            return Result.Failure("Не все игроки готовы.");

        _seed = Random.Shared.Next();
        _assignments = BuildAssignments();
        var payload = JsonSerializer.Serialize(new StartGameMessage(_seed.Value, _assignments));
        return _coordinator.SendToAll(BattleMessageTypes.StartGame, payload);
    }

    /// <summary>Назначения классов (без отправки; хост собирает при старте).</summary>
    public IReadOnlyList<PlayerAssignment> GetAssignments()
    {
        return BuildAssignments();
    }

    /// <summary>
    /// Отображения игроков (имя + цвет по слоту) для мультиплеерного боя.
    /// Порядок соответствует Assignments и Snapshot.Players.
    /// </summary>
    public IReadOnlyList<PlayerPresentation> BuildPresentations()
    {
        var players = _coordinator.Snapshot?.Players ?? [];
        var presentations = new List<PlayerPresentation>();
        for (int i = 0; i < players.Count; i++)
            presentations.Add(new PlayerPresentation(players[i].Name, PlayerColors.Get(i)));
        return presentations;
    }

    private Result SelectClass(string classId)
    {
        _localClassId = classId;
        _classByPlayerId[_coordinator.LocalPlayerId] = classId;
        return _coordinator.SendToHost(BattleMessageTypes.PlayerSelectClass,
            JsonSerializer.Serialize(new SelectClassMessage(classId)));
    }

    private void OnGameMessage(TransportMessage message)
    {
        switch (message.Type)
        {
            case BattleMessageTypes.PlayerSelectClass:
                HandleSelectClass(message.SenderId, message.Payload);
                break;
            case BattleMessageTypes.StartGame:
                HandleStartGame(message.Payload);
                break;
        }
    }

    private void HandleSelectClass(string senderId, string payload)
    {
        var msg = JsonSerializer.Deserialize<SelectClassMessage>(payload);
        if (msg is null) return;
        _classByPlayerId[senderId] = msg.ClassId;
    }

    private void HandleStartGame(string payload)
    {
        var msg = JsonSerializer.Deserialize<StartGameMessage>(payload);
        if (msg is null) return;
        _seed = msg.Seed;
        _assignments = msg.Assignments;
        foreach (var assignment in msg.Assignments)
            _classByPlayerId[assignment.PlayerId] = assignment.ClassId;
        GameStarted?.Invoke();
    }

    private IReadOnlyList<PlayerAssignment> BuildAssignments()
    {
        var players = _coordinator.Snapshot?.Players ?? [];
        var assignments = new List<PlayerAssignment>();
        for (int i = 0; i < players.Count; i++)
        {
            string classId = _classByPlayerId.TryGetValue(players[i].PlayerId, out var selected)
                ? selected
                : _heroIds[i % _heroIds.Length];
            assignments.Add(new PlayerAssignment(players[i].PlayerId, classId));
        }
        return assignments;
    }
}