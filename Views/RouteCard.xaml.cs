using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ShinySuite.ViewModels;

namespace ShinySuite.Views;

public partial class RouteCard : UserControl
{
    public RouteCard()
    {
        InitializeComponent();
    }

    // Redirect vertical wheel events from the horizontal category ScrollViewer
    // up to the parent so the outer route ListBox can scroll normally.
    private void CategoryScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled) return;
        e.Handled = true;
        var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = MouseWheelEvent,
            Source      = sender,
        };
        ((UIElement)((ScrollViewer)sender).Parent).RaiseEvent(args);
    }

    // ── Manual count entry ─────────────────────────────────────────────────────

    private void CountBox_GotFocus(object sender, RoutedEventArgs e)
        => ((TextBox)sender).SelectAll();

    private void CountBox_LostFocus(object sender, RoutedEventArgs e)
        => CommitCount((TextBox)sender);

    private void CountBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var tb = (TextBox)sender;
        if (e.Key == Key.Enter)
        {
            CommitCount(tb);
            tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            if (tb.DataContext is PokemonTileViewModel vm)
                tb.Text = vm.Count.ToString();
            tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            e.Handled = true;
        }
        else if (!IsAllowedCountKey(e.Key))
        {
            e.Handled = true;
        }
    }

    private static bool IsAllowedCountKey(Key k) =>
        k is (>= Key.D0 and <= Key.D9)
          or (>= Key.NumPad0 and <= Key.NumPad9)
          or Key.Back or Key.Delete
          or Key.Left or Key.Right or Key.Home or Key.End
          or Key.Tab;

    private static void CommitCount(TextBox tb)
    {
        if (tb.DataContext is PokemonTileViewModel vm)
        {
            if (int.TryParse(tb.Text, out int v))
                vm.SetCount(v);
            else
                tb.Text = vm.Count.ToString();
        }
    }
}
