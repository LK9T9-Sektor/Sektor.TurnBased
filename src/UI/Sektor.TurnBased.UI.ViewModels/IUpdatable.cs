namespace Sektor.TurnBased.UI.ViewModels;

/// <summary>
/// VM, требующая периодического обновления (мультиплеер: прокачка транспорта и
/// применение входящих команд). Хост (таймер WPF) вызывает Update для текущего экрана.
/// </summary>
public interface IUpdatable
{
    void Update();
}