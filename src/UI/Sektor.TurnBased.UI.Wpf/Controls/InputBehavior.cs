using System.Windows;
using System.Windows.Input;

namespace Sektor.TurnBased.UI.Wpf.Controls;

/// <summary>
/// Присоединённые команды на клики: RightClickCommand (инфо о юните)
/// и LeftClickCommand (выбор цели). Параметр — DataContext элемента.
/// </summary>
public static class InputBehavior
{
    public static readonly DependencyProperty RightClickCommandProperty = DependencyProperty.RegisterAttached(
        "RightClickCommand",
        typeof(ICommand),
        typeof(InputBehavior),
        new PropertyMetadata(null, OnCommandChanged));

    public static readonly DependencyProperty LeftClickCommandProperty = DependencyProperty.RegisterAttached(
        "LeftClickCommand",
        typeof(ICommand),
        typeof(InputBehavior),
        new PropertyMetadata(null, OnCommandChanged));

    public static ICommand? GetRightClickCommand(DependencyObject obj) =>
        (ICommand?)obj.GetValue(RightClickCommandProperty);

    public static void SetRightClickCommand(DependencyObject obj, ICommand? value) =>
        obj.SetValue(RightClickCommandProperty, value);

    public static ICommand? GetLeftClickCommand(DependencyObject obj) =>
        (ICommand?)obj.GetValue(LeftClickCommandProperty);

    public static void SetLeftClickCommand(DependencyObject obj, ICommand? value) =>
        obj.SetValue(LeftClickCommandProperty, value);

    private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
            return;

        if (e.OldValue is not null)
        {
            element.MouseRightButtonUp -= OnRightClick;
            element.MouseLeftButtonUp -= OnLeftClick;
        }

        if (e.NewValue is not null)
        {
            element.MouseRightButtonUp += OnRightClick;
            element.MouseLeftButtonUp += OnLeftClick;
        }
    }

    private static void OnRightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DependencyObject element)
            return;
        var parameter = element is FrameworkElement fe ? fe.DataContext : null;
        Execute(GetRightClickCommand(element), parameter);
    }

    private static void OnLeftClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DependencyObject element)
            return;
        var parameter = element is FrameworkElement fe ? fe.DataContext : null;
        Execute(GetLeftClickCommand(element), parameter);
    }

    private static void Execute(ICommand? command, object? parameter)
    {
        if (command is null || command.CanExecute(parameter) is false)
            return;
        command.Execute(parameter);
    }
}
