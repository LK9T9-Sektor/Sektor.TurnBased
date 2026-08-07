using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.Dialog.Commands;
using Sektor.TurnBased.UI.Core;
using Sektor.TurnBased.UI.ViewModels.Navigation;
using Sektor.TurnBased.UI.ViewModels.Shared;

namespace Sektor.TurnBased.UI.ViewModels.Dialog;

/// <summary>
/// VM диалога (квеста): показывает узел и варианты, проигрывает визуальные события
/// и обрабатывает выбор ответа. Общение только через INPC и команды.
/// </summary>
public sealed partial class DialogViewModel : ObservableObject, IGameViewModel
{
    private static readonly TimeSpan VisualDelay = TimeSpan.FromMilliseconds(120);

    private readonly DialogSession _session;
    private readonly NavigationManager _navigation;
    private readonly IReadOnlyDictionary<string, Func<VisualEvent, string>> _formatters;
    private readonly ObservableCollection<string> _logLines = new();
    private int _logCount;

    [ObservableProperty]
    private DialogSnapshot? snapshot;

    [ObservableProperty]
    private string status = "Квест начинается...";

    [ObservableProperty]
    private string? error;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>Лента визуальных событий (последние сверху).</summary>
    public ObservableCollection<string> EventFeed { get; } = new();

    /// <summary>Полный текстовый журнал игры.</summary>
    public IReadOnlyList<string> LogLines => _logLines;

    public DialogViewModel(DialogSession session, NavigationManager navigation)
    {
        _session = session;
        _navigation = navigation;
        _formatters = new Dictionary<string, Func<VisualEvent, string>>
        {
            ["NodeText"] = v => $"Узел: {DisplayNames.Humanize(v.SourceRuntimeId)}",
            ["Choice"] = v => $"Выбор: {v.Payload}",
            ["Ending"] = v => $"Исход: {DisplayNames.Humanize(v.SourceRuntimeId)}",
            ["SubDialogEnter"] = _ => "Вход в под-диалог",
            ["SubDialogComplete"] = _ => "Под-диалог завершён",
        };
    }

    /// <summary>Запускает квест и проигрывает стартовые визуальные события.</summary>
    public Task RunAsync() => StepAsync(_session.Start);

    [RelayCommand]
    private async Task Choose(ChoiceOption? choice)
    {
        if (IsBusy || choice is null || Snapshot?.NodeId is null)
            return;

        var nodeId = Snapshot.NodeId;
        await StepAsync(() => _session.Submit(new ChooseOptionCommand(nodeId, choice.ChoiceId)));
    }

    [RelayCommand]
    private void GoToLobby() => _navigation.NavigateTo(Pages.Lobby);

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

    private void RefreshSnapshot()
    {
        Snapshot = (DialogSnapshot)_session.Snapshot();
        Status = BuildStatus();
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
            return $"Исход: {DisplayNames.Humanize(Snapshot?.OutcomeNodeId ?? "—")}";

        return "Квест";
    }
}
