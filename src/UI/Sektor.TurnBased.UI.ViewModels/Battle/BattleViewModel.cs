using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sektor.TurnBased.Battle.Commands;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.UI.Core;
using Sektor.TurnBased.UI.Core.Multiplayer;
using Sektor.TurnBased.UI.ViewModels.Navigation;
using Sektor.TurnBased.UI.ViewModels.Shared;

namespace Sektor.TurnBased.UI.ViewModels.Battle;

/// <summary>
/// VM боя: обновляет снапшот, карточки юнитов и статус, проигрывает визуальные
/// события поочерёдно и управляет выбором действия и цели с подтверждением.
/// В мультиплеере применяет входящие команды через Update (таймер) и обновляет экран.
/// Общение только через INPC и команды, без событий и messenger.
/// </summary>
public sealed partial class BattleViewModel : ObservableObject, IGameViewModel, IUpdatable
{
    private static readonly TimeSpan VisualDelay = TimeSpan.FromMilliseconds(140);

    /// <summary>Длительность анимации всплывающего текста.</summary>
    private static readonly TimeSpan FloatingDuration = TimeSpan.FromMilliseconds(1050);

    /// <summary>Высота подъёма всплывающего текста в пикселях.</summary>
    private const double FloatingRisePx = 36;

    /// <summary>Частота обновления анимации всплывающего текста.</summary>
    private const int FloatingFps = 30;

    private readonly BattleSession _session;
    private readonly INetworkedBattleSession? _networked;
    private readonly NavigationManager _navigation;
    private readonly UnitInfoViewModel _unitInfo;
    private readonly AbilityInfoViewModel _abilityInfo;
    private readonly ConfirmationViewModel _confirmation;
    private readonly SettingsViewModel _settings;
    private readonly IReadOnlyDictionary<string, Func<VisualEvent, string>> _formatters;
    private readonly BattleLogViewModel _log = new();

    /// <summary>Всплывающие тексты по акторам: коллекция переживает пересоздание карточек.</summary>
    private readonly Dictionary<string, ObservableCollection<FloatingTextViewModel>> _floatingTexts = new();

    /// <summary>Id жизненного стата (для фильтра: всплывает только урон/лечение по здоровью).</summary>
    private string? _deathStatId;

    [ObservableProperty]
    private BattleSnapshot? snapshot;

    [ObservableProperty]
    private IReadOnlyList<UnitCardViewModel> playerUnits = Array.Empty<UnitCardViewModel>();

    [ObservableProperty]
    private IReadOnlyList<UnitCardViewModel> enemyUnits = Array.Empty<UnitCardViewModel>();

    [ObservableProperty]
    private string status = "Бой начинается...";

    [ObservableProperty]
    private string? error;

    [ObservableProperty]
    private string? hint;

    [ObservableProperty]
    private string? selectedTargetId;

    [ObservableProperty]
    private ActionOption? pendingAction;

    [ObservableProperty]
    private bool isAwaitingTarget;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>Очередь ходов текущего раунда (общий контрол для всех раскладок боя).</summary>
    public ObservableCollection<TurnOrderItemViewModel> TurnQueue { get; } = new();

    /// <summary>Плитки доступных действий текущего игрока (квадраты с глифом и именем).</summary>
    public ObservableCollection<ActionTileViewModel> ActionTiles { get; } = new();

    /// <summary>Общие настройки UX (подтверждение хода, пульсация, виньетка).</summary>
    public SettingsViewModel Settings => _settings;

    /// <summary>true — бой в одну линию (стиль Blades), false — две линии с карточками.</summary>
    public bool IsSingleLineLayout => _session.Kind == GameKinds.BattleLine;

    /// <summary>Лог боя: события и журнал (открывается кнопкой в шапке).</summary>
    public BattleLogViewModel Log => _log;

    public BattleViewModel(
        BattleSession session,
        NavigationManager navigation,
        UnitInfoViewModel unitInfo,
        AbilityInfoViewModel abilityInfo,
        ConfirmationViewModel confirmation,
        SettingsViewModel settings)
    {
        _session = session;
        _networked = session as INetworkedBattleSession;
        if (_networked is not null)
            _networked.StateChanged += OnRemoteStateChanged;
        _navigation = navigation;
        _unitInfo = unitInfo;
        _abilityInfo = abilityInfo;
        _confirmation = confirmation;
        _settings = settings;
        _formatters = new Dictionary<string, Func<VisualEvent, string>>
        {
            ["TurnStart"] = v => $"Ход: {ActorName(v.SourceRuntimeId)}",
            ["StatChanged"] = v => $"{ActorName(v.TargetRuntimeId ?? string.Empty)}: {v.Value}",
            ["Died"] = v => $"{ActorName(v.TargetRuntimeId ?? string.Empty)} погиб",
            ["Summon"] = v => $"{ActorName(v.SourceRuntimeId)} призван",
            ["TurnBlocked"] = v => $"{ActorName(v.SourceRuntimeId)} пропускает ход",
            ["TurnSkipped"] = v => $"{ActorName(v.SourceRuntimeId)} пропускает ход",
            ["StatusApply"] = v => $"{ActorName(v.TargetRuntimeId ?? string.Empty)}: статус {v.Payload}",
        };
    }

    /// <summary>Запускает бой и проигрывает стартовые визуальные события.</summary>
    public Task RunAsync() => StepAsync(_session.Start);

    /// <summary>
    /// Периодический апдейт (таймер хоста): прокачивает транспорт и применяет
    /// входящие команды в мультиплеере.
    /// </summary>
    public void Update() => _networked?.Update();

    private void OnRemoteStateChanged() => _ = StepRemoteAsync();

    [RelayCommand]
    private void ShowUnitInfo(UnitCardViewModel? card)
    {
        if (card is not null)
            _unitInfo.Show(card.Unit);
    }

    [RelayCommand]
    private void ShowActionInfo(ActionTileViewModel? tile)
    {
        if (tile is not null)
            _abilityInfo.Show(tile);
    }

    [RelayCommand]
    private void ChooseAction(ActionTileViewModel? tile)
    {
        if (IsBusy || tile is null || Snapshot is null)
            return;

        var option = tile.Option;
        Hint = null;

        if (option.TargetMode == BattleTargetModes.SingleEnemy)
        {
            PendingAction = option;
            IsAwaitingTarget = true;
            UpdateActionSelection();
            return;
        }

        var targets = ResolveTargets(option);
        if (targets is null)
            return;

        PendingAction = null;
        IsAwaitingTarget = false;
        UpdateActionSelection();
        ConfirmAction(option, targets);
    }

    [RelayCommand(CanExecute = nameof(CanChooseTarget))]
    private void ChooseTarget(UnitCardViewModel? target)
    {
        if (target is null)
            return;

        if (PendingAction is null)
        {
            Hint = IsPlayerTurn ? "Сначала выберите действие." : "Сейчас ход противника.";
            return;
        }

        var option = PendingAction;
        PendingAction = null;
        IsAwaitingTarget = false;
        SelectedTargetId = null;
        Hint = null;
        UpdateActionSelection();
        ConfirmAction(option, new[] { target.Unit.RuntimeId });
    }

    [RelayCommand(CanExecute = nameof(CanEndTurn))]
    private void EndTurn()
    {
        if (Snapshot?.CurrentActorId is not { } actorId)
            return;

        var command = new SkipTurnCommand(actorId);
        if (_settings.ConfirmEndTurn)
            _confirmation.Request("Завершить ход?", () => StepAsync(() => _session.Submit(command)));
        else
            _ = StepAsync(() => _session.Submit(command));
    }

    [RelayCommand]
    private void GoToLobby() => _navigation.NavigateTo(Pages.Lobby);

    private bool CanChooseTarget(UnitCardViewModel? target) =>
        target is not null && target.Unit.IsAlive && target.Unit.TeamId != PlayerTeamId;

    private bool CanEndTurn() =>
        Snapshot is not null
        && !IsBusy
        && !IsAwaitingTarget
        && IsPlayerTurn;

    private bool IsPlayerTurn => Snapshot?.IsLocalTurn ?? false;

    private string PlayerTeamId =>
        Snapshot?.Actors.FirstOrDefault(a => a.IsHumanControlled)?.TeamId ?? "player";

    private IReadOnlyList<string>? ResolveTargets(ActionOption option)
    {
        if (option.TargetMode == BattleTargetModes.Self && Snapshot?.CurrentActorId is { } actorId)
            return new[] { actorId };

        if (option.TargetMode == BattleTargetModes.AllEnemies)
        {
            var enemies = Snapshot!.Actors
                .Where(a => a.TeamId != PlayerTeamId && a.IsAlive)
                .Select(a => a.RuntimeId)
                .ToList();
            return enemies.Count > 0 ? enemies : null;
        }

        return null;
    }

    private void ConfirmAction(ActionOption option, IReadOnlyList<string> targetIds)
    {
        if (Snapshot?.CurrentActorId is null)
            return;

        var targetText = string.Join(
            ", ",
            targetIds.Select(id => Snapshot!.Actors.FirstOrDefault(a => a.RuntimeId == id)?.DisplayName ?? id));
        var command = new UseActionCommand(Snapshot.CurrentActorId, option.ActionId, targetIds);
        _confirmation.Request($"{option.Name} → {targetText}", () => StepAsync(() => _session.Submit(command)));
    }

    private async Task StepAsync(Func<Result> step)
    {
        IsBusy = true;
        try
        {
            var result = step();
            if (result.IsFailure)
            {
                Error = result.Error;
                Status = $"Ошибка: {result.Error}";
                return;
            }

            await RefreshAfterStepAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Обновление после применения входящей команды: визуалы уже накоплены сессией.</summary>
    private async Task StepRemoteAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            await RefreshAfterStepAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshAfterStepAsync()
    {
        await AnimateVisualsAsync();
        RefreshSnapshot();
    }

    private async Task AnimateVisualsAsync()
    {
        foreach (var visual in _session.TakeVisuals())
        {
            _log.AddEvent(FormatVisual(visual));
            AddFloatingText(visual);
            await Task.Delay(VisualDelay);
        }
    }

    /// <summary>Добавляет всплывающий текст для изменения здоровья цели (урон/лечение).</summary>
    private void AddFloatingText(VisualEvent visual)
    {
        if (visual.EventType != "StatChanged"
            || visual.TargetRuntimeId is null
            || visual.Delta == 0)
            return;

        _deathStatId ??= Snapshot?.Actors.FirstOrDefault()?.VitalStat?.StatId;
        if (_deathStatId is not null && visual.StatId != _deathStatId)
            return;

        var floater = new FloatingTextViewModel(
            visual.TargetRuntimeId,
            Math.Abs(visual.Delta).ToString(),
            isHeal: visual.Delta > 0,
            isCrit: visual.IsCritical);
        var collection = FloatingTextsFor(visual.TargetRuntimeId);
        collection.Add(floater);
        _ = AnimateAndRemoveAsync(floater, collection);
    }

    /// <summary>Плавно поднимает и растворяет всплывающий текст, затем удаляет его.</summary>
    private static async Task AnimateAndRemoveAsync(
        FloatingTextViewModel floater,
        ObservableCollection<FloatingTextViewModel> collection)
    {
        var frames = (int)(FloatingDuration.TotalMilliseconds / 1000.0 * FloatingFps);
        var delay = TimeSpan.FromMilliseconds(1000.0 / FloatingFps);
        for (var i = 1; i <= frames; i++)
        {
            var t = i / (double)frames;
            floater.Opacity = 1.0 - t * t;
            floater.OffsetY = -FloatingRisePx * t;
            await Task.Delay(delay);
        }

        floater.Opacity = 0;
        collection.Remove(floater);
    }

    /// <summary>Общая коллекция всплывающих текстов для актора (создаётся при первом обращении).</summary>
    public ObservableCollection<FloatingTextViewModel> FloatingTextsFor(string runtimeId)
    {
        if (!_floatingTexts.TryGetValue(runtimeId, out var collection))
        {
            collection = new ObservableCollection<FloatingTextViewModel>();
            _floatingTexts.Add(runtimeId, collection);
        }
        return collection;
    }

    private string FormatVisual(VisualEvent visual) =>
        _formatters.TryGetValue(visual.EventType, out var format) ? format(visual) : visual.EventType;

    private string ActorName(string runtimeId) =>
        Snapshot?.Actors.FirstOrDefault(a => a.RuntimeId == runtimeId)?.DisplayName
        ?? DisplayNames.Humanize(runtimeId);

    private void RefreshSnapshot()
    {
        Snapshot = (BattleSnapshot)_session.Snapshot();
        var activeId = Snapshot.CurrentActorId;
        var playerTeamId = PlayerTeamId;
        PlayerUnits = Snapshot.Actors
            .Where(a => a.TeamId == playerTeamId)
            .Select(a => CreateCard(a, activeId))
            .ToList();
        EnemyUnits = Snapshot.Actors
            .Where(a => a.TeamId != playerTeamId)
            .Select(a => CreateCard(a, activeId))
            .ToList();
        Status = BuildStatus();
        PendingAction = null;
        IsAwaitingTarget = false;
        SelectedTargetId = null;
        RefreshActionTiles();
        RefreshTurnQueue();
        RefreshLog();
        EndTurnCommand.NotifyCanExecuteChanged();
    }

    private void RefreshActionTiles()
    {
        ActionTiles.Clear();
        if (Snapshot is not { } snap)
            return;

        foreach (var option in snap.AvailableActions)
            ActionTiles.Add(new ActionTileViewModel(option));
        UpdateActionSelection();
    }

    private void UpdateActionSelection()
    {
        var pendingId = PendingAction?.ActionId;
        foreach (var tile in ActionTiles)
            tile.IsSelected = tile.Option.ActionId == pendingId;
    }

    private void RefreshTurnQueue()
    {
        TurnQueue.Clear();
        if (Snapshot is not { } snap)
            return;

        var index = snap.TurnIndex;
        for (var i = 0; i < snap.TurnOrder.Count; i++)
        {
            var unit = snap.Actors.FirstOrDefault(a => a.RuntimeId == snap.TurnOrder[i]);
            if (unit is null)
                continue;
            TurnQueue.Add(new TurnOrderItemViewModel(unit, isActive: i == index, hasActed: i < index));
        }
    }

    private UnitCardViewModel CreateCard(UnitSnapshot unit, string? activeId)
    {
        var isSelected = IsAwaitingTarget && unit.RuntimeId == SelectedTargetId;
        var isSelectable = IsAwaitingTarget && unit.IsAlive && unit.TeamId != PlayerTeamId;
        return new UnitCardViewModel(
            unit,
            isActive: unit.RuntimeId == activeId,
            isSelected,
            isSelectable,
            FloatingTextsFor(unit.RuntimeId));
    }

    private void RefreshLog()
    {
        _log.SyncLog(_session.Log);
    }

    private string BuildStatus()
    {
        if (_session.IsFailed)
            return $"Ошибка: {_session.Error}";

        if (_session.IsFinished)
            return Snapshot?.WinnerTeamId is { } winner
                ? $"Победа: {DisplayNames.Humanize(winner)}"
                : "Ничья";

        var current = Snapshot?.Actors.FirstOrDefault(a => a.RuntimeId == Snapshot.CurrentActorId);
        return $"Раунд {Snapshot?.RoundNumber} · Ход: {current?.DisplayName ?? "—"}";
    }
}
