using CommunityToolkit.Mvvm.ComponentModel;

namespace Sektor.TurnBased.UI.ViewModels.Navigation;

/// <summary>
/// Менеджер навигации в пределах одного окна: хранит зарегистрированные страницы
/// и текущий объект (VM). Реагирует через INPC, без событий — вьюха подписывается
/// биндингом на Current, будущий хост (Unity) — тем же способом.
/// </summary>
public sealed class NavigationManager : ObservableObject
{
    private readonly Dictionary<string, object> _pages = new();
    private object? _current;

    /// <summary>Текущая VM (null — не переходили).</summary>
    public object? Current
    {
        get => _current;
        private set => SetProperty(ref _current, value);
    }

    /// <summary>Регистрирует страницу по Id (используется в RootViewModel для лобби).</summary>
    public void Register(string pageId, object viewModel) => _pages[pageId] = viewModel;

    /// <summary>Переход на зарегистрированную страницу по Id.</summary>
    public void NavigateTo(string pageId)
    {
        if (_pages.TryGetValue(pageId, out var viewModel))
            Current = viewModel;
    }

    /// <summary>Переход на конкретную VM (игровые экраны создаются по мере запуска).</summary>
    public void NavigateTo(object viewModel) => Current = viewModel;
}
