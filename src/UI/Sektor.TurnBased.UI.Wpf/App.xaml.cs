using System.Windows;
using Sektor.TurnBased.UI.Core;
using Sektor.TurnBased.UI.ViewModels.Navigation;
using Sektor.TurnBased.UI.ViewModels.Shared;

namespace Sektor.TurnBased.UI.Wpf;

/// <summary>
/// Точка входа WPF-хоста: собирает корневую VM (навигация + общие контролы + лобби)
/// и открывает единственное окно. Все экраны — UserControl-ы через VM.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var navigation = new NavigationManager();
        var unitInfo = new UnitInfoViewModel();
        var confirmation = new ConfirmationViewModel();
        var root = new RootViewModel(navigation, unitInfo, confirmation, GameSessionFactory.Create);

        var window = new MainWindow { DataContext = root };
        MainWindow = window;
        window.Show();
    }
}
