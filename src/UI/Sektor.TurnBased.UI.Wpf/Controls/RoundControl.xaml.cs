using System.Windows;
using System.Windows.Controls;

namespace Sektor.TurnBased.UI.Wpf.Controls;

/// <summary>
/// Общий индикатор номера раунда: золотое кольцо с числом (стиль Blades).
/// Значение задаётся свойством Value.
/// </summary>
public partial class RoundControl : UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(int),
        typeof(RoundControl),
        new PropertyMetadata(0));

    /// <summary>Отображаемый номер раунда.</summary>
    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public RoundControl()
    {
        InitializeComponent();
    }
}
