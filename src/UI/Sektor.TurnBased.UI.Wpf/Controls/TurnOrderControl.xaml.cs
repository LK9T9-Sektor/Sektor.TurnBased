using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Sektor.TurnBased.UI.Wpf.Controls;

/// <summary>
/// Общая очередь ходов: горизонтальная лента иконок юнитов в порядке хода.
/// Активный ход подсвечен стрелкой, уже сходившие затемнены. Переиспользуется
/// всеми раскладками боя через ItemsSource.
/// </summary>
public partial class TurnOrderControl : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(TurnOrderControl),
        new PropertyMetadata(null));

    /// <summary>Элементы очереди ходов (см. TurnOrderItemViewModel).</summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public TurnOrderControl()
    {
        InitializeComponent();
    }
}
