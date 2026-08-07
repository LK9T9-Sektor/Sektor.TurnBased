using System.Windows;

namespace Sektor.TurnBased.UI.Wpf;

/// <summary>Главное окно хоста: одна Window, все экраны — UserControl-ы через VM.</summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
