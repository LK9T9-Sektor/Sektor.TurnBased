using System.Windows;
using System.Windows.Controls;
using Sektor.TurnBased.UI.ViewModels.Multiplayer;

namespace Sektor.TurnBased.UI.Wpf.Views;

/// <summary>
/// Экран мультиплеер-лобби.
/// </summary>
public partial class MultiplayerLobbyView : UserControl
{
    public MultiplayerLobbyView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MultiplayerLobbyViewModel vm)
            vm.SetClipboardCallback(text => Clipboard.SetText(text));
    }
}
