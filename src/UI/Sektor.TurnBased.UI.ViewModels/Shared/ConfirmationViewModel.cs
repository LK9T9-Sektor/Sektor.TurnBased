using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sektor.TurnBased.UI.ViewModels.Shared;

/// <summary>
/// Общий контрол подтверждения действия (например, конца хода): запрашивает
/// подтверждение и исполняет переданный обработчик после согласия. Без событий —
/// просто состояние + команды.
/// </summary>
public sealed partial class ConfirmationViewModel : ObservableObject
{
    [ObservableProperty]
    private string? message;

    private Func<Task>? _onConfirm;

    /// <summary>true — панель подтверждения открыта.</summary>
    public bool IsOpen => _onConfirm is not null;

    /// <summary>Открывает подтверждение с текстом и обработчиком согласия.</summary>
    public void Request(string text, Func<Task> onConfirm)
    {
        Message = text;
        _onConfirm = onConfirm;
        OnPropertyChanged(nameof(IsOpen));
    }

    [RelayCommand]
    private async Task Confirm()
    {
        var handler = _onConfirm;
        _onConfirm = null;
        Message = null;
        OnPropertyChanged(nameof(IsOpen));

        if (handler is not null)
            await handler();
    }

    [RelayCommand]
    private void Cancel()
    {
        _onConfirm = null;
        Message = null;
        OnPropertyChanged(nameof(IsOpen));
    }
}
