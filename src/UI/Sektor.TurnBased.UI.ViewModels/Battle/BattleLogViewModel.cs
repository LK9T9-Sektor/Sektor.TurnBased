using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sektor.TurnBased.UI.ViewModels.Battle;

/// <summary>
/// VM отдельного контрола лога: лента визуальных событий и полный журнал.
/// Открывается кнопкой в шапке боя, чтобы не занимать экран постоянно.
/// </summary>
public sealed partial class BattleLogViewModel : ObservableObject
{
    private const int MaxEventFeed = 80;
    private readonly List<string> _logLines = new();
    private int _logCount;

    /// <summary>Последние визуальные события (новые сверху).</summary>
    public ObservableCollection<string> EventFeed { get; } = new();

    /// <summary>Полный журнал хода боя.</summary>
    public IReadOnlyList<string> LogLines => _logLines;

    /// <summary>true — панель лога открыта.</summary>
    [ObservableProperty]
    private bool isOpen;

    /// <summary>Добавляет визуальное событие (новое — сверху списка).</summary>
    public void AddEvent(string text)
    {
        EventFeed.Insert(0, text);
        while (EventFeed.Count > MaxEventFeed)
            EventFeed.RemoveAt(EventFeed.Count - 1);
    }

    /// <summary>Дописывает новые строки журнала движка (только добавленные).</summary>
    public void SyncLog(IReadOnlyList<string> log)
    {
        while (_logCount < log.Count)
            _logLines.Add(log[_logCount++]);
    }

    [RelayCommand]
    private void Toggle() => IsOpen = !IsOpen;

    [RelayCommand]
    private void Close() => IsOpen = false;
}
