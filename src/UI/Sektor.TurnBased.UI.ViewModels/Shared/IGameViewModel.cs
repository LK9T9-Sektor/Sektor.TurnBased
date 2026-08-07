namespace Sektor.TurnBased.UI.ViewModels.Shared;

/// <summary>
/// Игровая VM, способная запустить сессию после навигации из лобби.
/// </summary>
public interface IGameViewModel
{
    /// <summary>Запускает игру и проигрывает визуальные события до первого ввода.</summary>
    Task RunAsync();
}
