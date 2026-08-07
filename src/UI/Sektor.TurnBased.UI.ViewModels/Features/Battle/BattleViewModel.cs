using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sektor.TurnBased.Battle.Commands;
using Sektor.TurnBased.Battle.Model;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.UI.Core;
using Sektor.TurnBased.UI.ViewModels.Navigation;
using Sektor.TurnBased.UI.ViewModels.Shared;

namespace Sektor.TurnBased.UI.ViewModels.Battle;

/// <summary>
/// VM боя: обновляет снапшот, список юнитов и статус, проигрывает визуальные
/// события поочерёдно и управляет выбором действия и цели с подтверждением.
/// Общение только через INPC и команды, без событий и messenger.
/// </summary>
public sealed partial class BattleViewModel : ObservableObject, IGameViewModel
{
    private static readonly TimeSpan VisualDelay = TimeSpan.FromMilliseconds(140);

    private readonly BattleSession _session;
    private readonly NavigationManager _navigation;
    private readonly UnitInfoViewModel _unitInfo;
    private readonly ConfirmationViewModel _confirmation;
    private readonly IReadOnlyDictionary<string, Func<VisualEvent, string>> _formatters;
    private readonly ObservableCollection<string> _logLines = new();
    private int _logCount;

    [ObservableProperty]
    private BattleSnapshot? snapshot;

    [ObservableProperty]
    private IReadOnlyList<UnitSnapshot> playerUnits = Array.Empty<UnitSnapshot>();

    [ObservableProperty]
    private IReadOnlyList<UnitSnapshot> enemyUnits = Array.Empty<UnitSnapshot>();

    [ObservableProperty]
    private string status = "Бой начинается...";

    [ObservableProperty]
    private string? error;

    [ObservableProperty]
    private ActionOption? pendingAction;

    [ObservableProperty]
    private bool isAwaitingTarget;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>Лента визуальных событий (последние сверху).</summary>
    public ObservableCollection<string> EventFeed { get; } = new();

    /// <summary>Полный текстовый журнал игры.</summary>
    public IReadOnlyList<string> LogLines => _logLines;

    public BattleViewModel(
        BattleSession session,
        NavigationManager navigation,
        UnitInfoViewModel unitInfo,
        ConfirmationViewModel confirmation)
    {
        _session = session;
        _navigation = navigation;
        _unitInfo = unitInfo;
        _confirmation = confirmation;
        _formatters = new Dictionary<string, Func<VisualEvent, string>>
        {
            ["TurnStart"] = v => $"Ход: {ActorName(v.SourceRuntimeId)}",
            ["StatChanged"] = v => $"{ActorName(v.TargetRuntimeId ?? string.Empty)}: {v.Value}",
            ["Died"] = v => $"{ActorName(v.TargetRuntimeId ?? string.Empty)} погиб",
            ["Summon"] = v => $"{ActorName(v.SourceRuntimeId)} призван",
            ["TurnBlocked"] = v => $"{ActorName(v.SourceRuntimeId)} пропускает ход",
            ["StatusApply"] = v => $"{ActorName(v.TargetRuntimeId ?? string.Empty)}: статус {v.Payload}",
        };
    }

    /// <summary>Запускает бой и проигрывает стартовые визуальные события.</summary>
    public Task RunAsync() => StepAsync(_session.Start);

    [RelayCommand]
    private void ShowUnitInfo(UnitSnapshot? unit)
    {
        if (unit is not null)
            _unitInfo.Show(unit);
    }

    [RelayCommand]
    private void ChooseAction(ActionOption? option)
    {
        if (IsBusy || option is null || Snapshot is null)
            return;

        if (option.TargetMode == BattleTargetModes.SingleEnemy)
        {
            PendingAction = option;
            IsAwaitingTarget = true;
            return;
        }

        var targets = ResolveTargets(option);
        if (targets is null)
            return;

        PendingAction = null;
        IsAwaitingTarget = false;
        ConfirmAction(option, targets);
    }

    [RelayCommand(CanExecute = nameof(CanChooseTarget))]
    private void ChooseTarget(UnitSnapshot? target)
    {
        if (target is null || PendingAction is null)
            return;

        var option = PendingAction;
        PendingAction = null;
        IsAwaitingTarget = false;
        ConfirmAction(option, new[] { target.RuntimeId });
    }

    [RelayCommand]
    private void GoToLobby() => _navigation.NavigateTo(Pages.Lobby);

    private bool CanChooseTarget(UnitSnapshot? target) =>
        IsAwaitingTarget && target is not null && target.IsAlive && target.TeamId != PlayerTeamId;

    private string PlayerTeamId =>
        Snapshot?.Actors.FirstOrDefault(a => a.ControlledBy == "player")?.TeamId ?? "player";

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

            await AnimateVisualsAsync();
            RefreshSnapshot();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AnimateVisualsAsync()
    {
        foreach (var visual in _session.TakeVisuals())
        {
            EventFeed.Insert(0, FormatVisual(visual));
            while (EventFeed.Count > 80)
                EventFeed.RemoveAt(EventFeed.Count - 1);
            await Task.Delay(VisualDelay);
        }
    }

    private string FormatVisual(VisualEvent visual) =>
        _formatters.TryGetValue(visual.EventType, out var format) ? format(visual) : visual.EventType;

    private string ActorName(string runtimeId) =>
        Snapshot?.Actors.FirstOrDefault(a => a.RuntimeId == runtimeId)?.DisplayName
        ?? DisplayNames.Humanize(runtimeId);

    private void RefreshSnapshot()
    {
        Snapshot = (BattleSnapshot)_session.Snapshot();
        PlayerUnits = Snapshot.Actors.Where(a => a.TeamId == PlayerTeamId).ToList();
        EnemyUnits = Snapshot.Actors.Where(a => a.TeamId != PlayerTeamId).ToList();
        Status = BuildStatus();
        PendingAction = null;
        IsAwaitingTarget = false;
        RefreshLog();
    }

    private void RefreshLog()
    {
        while (_logCount < _session.Log.Count)
            _logLines.Add(_session.Log[_logCount++]);
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
