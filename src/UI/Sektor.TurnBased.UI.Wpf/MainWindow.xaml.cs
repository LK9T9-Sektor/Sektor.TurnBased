using System.Windows;
using System.Windows.Threading;
using Sektor.TurnBased.UI.ViewModels;
using Sektor.TurnBased.UI.ViewModels.Navigation;

namespace Sektor.TurnBased.UI.Wpf;

/// <summary>
/// Главное окно хоста: одна Window, все экраны — UserControl-ы через VM.
/// Таймер прокачивает IUpdatable текущего экрана (мультиплеер: транспорт и бой).
/// </summary>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timer;

    public MainWindow()
    {
        InitializeComponent();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (DataContext is RootViewModel root && root.Navigation.Current is IUpdatable updatable)
            updatable.Update();
    }
}