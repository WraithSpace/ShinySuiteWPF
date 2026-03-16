using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ShinySuite.Services;
using ShinySuite.ViewModels;
using ShinySuite.Views;

namespace ShinySuite;

public partial class MainWindow : Window
{
    private MainViewModel VM => (MainViewModel)DataContext;
    private MiniPlayerWindow? _mini;
    private bool _sidebarExpanded = true;

    public MainWindow()
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);
        var vm = new MainViewModel();
        DataContext = vm;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.Filtered))
                RefreshDropdown();
        };
        vm.PopOutRequested += route =>
        {
            if (_mini is not null && _mini.IsLoaded && _mini.DataContext == route)
            {
                if (_mini.IsVisible) _mini.Hide(); else _mini.Show();
                return;
            }
            _mini?.Close();
            _mini = new MiniPlayerWindow { DataContext = route, Width = 200 * VM.UiScale };
            _mini.Left = Left + 20;
            _mini.Top  = Top  + 20;
            _mini.Show();
        };
        StateChanged += (_, _) =>
            MaximizeBtn.Content = WindowState == WindowState.Maximized
                ? "\uE923" : "\uE922"; // ChromeRestore : ChromeMaximize
        LocationChanged += (_, _) => HideDropdown();
        SizeChanged     += (_, _) => HideDropdown();
        Closing += (_, _) =>
        {
            var isMax = WindowState == WindowState.Maximized;
            VM.Config.WindowWidth     = isMax ? RestoreBounds.Width  : Width;
            VM.Config.WindowHeight    = isMax ? RestoreBounds.Height : Height;
            VM.Config.WindowLeft      = isMax ? RestoreBounds.Left   : Left;
            VM.Config.WindowTop       = isMax ? RestoreBounds.Top    : Top;
            VM.Config.WindowMaximized = isMax;
            VM.Config.SidebarExpanded = _sidebarExpanded;
            VM.StopDetection();
            VM.SaveConfig();
            _mini?.Close();
        };

        // Restore saved window geometry
        var cfg = vm.Config;
        if (cfg.WindowWidth  > 0) Width  = cfg.WindowWidth;
        if (cfg.WindowHeight > 0) Height = cfg.WindowHeight;
        if (cfg.WindowLeft.HasValue && cfg.WindowTop.HasValue)
        {
            WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
            Left = cfg.WindowLeft.Value;
            Top  = cfg.WindowTop.Value;
        }
        if (cfg.WindowMaximized) WindowState = WindowState.Maximized;

        // Restore sidebar state (adjustWidth: false — width already saved correctly)
        _sidebarExpanded = cfg.SidebarExpanded;
        if (!_sidebarExpanded)
            ApplySidebarState(adjustWidth: false);

        // Scroll to top when a route moves to position 0 (Start pressed)
        vm.Routes.CollectionChanged += (_, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move
                && e.NewStartingIndex == 0)
                Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
                    FindScrollViewer(RoutesList)?.ScrollToTop());
        };
    }

    // ── Sidebar toggle ──────────────────────────────────────────────────────────

    private void SidebarToggle_Click(object sender, RoutedEventArgs e)
    {
        _sidebarExpanded = !_sidebarExpanded;
        ApplySidebarState();
    }

    private void ApplySidebarState(bool adjustWidth = true)
    {
        const double expandedW = 200;
        const double collapsedW = 48;
        const double delta = expandedW - collapsedW; // 152

        if (adjustWidth && WindowState != WindowState.Maximized)
            Width += _sidebarExpanded ? delta : -delta;

        if (_sidebarExpanded)
        {
            SidebarCol.Width = new GridLength(expandedW);
            SidebarToggle.Content = "\uE76B"; // ChevronLeft
            SearchInputArea.Visibility = Visibility.Visible;
            SearchAddBtn.Visibility = Visibility.Visible;
            GameSelectorWrap.Visibility = Visibility.Visible;
            ScaleSelectorWrap.Visibility = Visibility.Visible;
            StatsBorder.Visibility = Visibility.Visible;
            LocationsLabel.Visibility = Visibility.Visible;
            OcrLabel.Visibility = Visibility.Visible;
            HistoryLabel.Visibility = Visibility.Visible;
            SetNavBtnExpanded(LocationsBtn);
            SetNavBtnExpanded(OcrBtn);
            SetNavBtnExpanded(HistoryBtn);
        }
        else
        {
            SidebarCol.Width = new GridLength(collapsedW);
            SidebarToggle.Content = "\uE76C"; // ChevronRight
            SearchInputArea.Visibility = Visibility.Collapsed;
            SearchAddBtn.Visibility = Visibility.Collapsed;
            GameSelectorWrap.Visibility = Visibility.Collapsed;
            ScaleSelectorWrap.Visibility = Visibility.Collapsed;
            StatsBorder.Visibility = Visibility.Collapsed;
            LocationsLabel.Visibility = Visibility.Collapsed;
            OcrLabel.Visibility = Visibility.Collapsed;
            HistoryLabel.Visibility = Visibility.Collapsed;
            SetNavBtnCollapsed(LocationsBtn);
            SetNavBtnCollapsed(OcrBtn);
            SetNavBtnCollapsed(HistoryBtn);
        }
    }

    private static void SetNavBtnExpanded(Button btn)
    {
        btn.Padding = new Thickness(10, 0, 10, 0);
        btn.HorizontalContentAlignment = HorizontalAlignment.Left;
    }

    private static void SetNavBtnCollapsed(Button btn)
    {
        btn.Padding = new Thickness(0);
        btn.HorizontalContentAlignment = HorizontalAlignment.Center;
    }

    // ── Search dropdown ────────────────────────────────────────────────────────

    private void SearchBox_GotFocus(object sender, RoutedEventArgs e) => RefreshDropdown();

    private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            if (!SearchBox.IsFocused && !SuggestionsList.IsFocused)
                HideDropdown();
        });
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                HideDropdown();
                break;

            case Key.Down when SuggestionsDropdown.IsVisible && SuggestionsList.Items.Count > 0:
                SuggestionsList.Focus();
                SuggestionsList.SelectedIndex = 0;
                e.Handled = true;
                break;

            case Key.Enter:
                var first = SuggestionsList.SelectedItem as string
                         ?? (SuggestionsList.Items.Count > 0 ? SuggestionsList.Items[0] as string : null);
                SelectLocation(first);
                e.Handled = true;
                break;
        }
    }

    private void SuggestionsList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var fe = e.OriginalSource as FrameworkElement;
        while (fe is not null && fe is not ListBoxItem)
            fe = VisualTreeHelper.GetParent(fe) as FrameworkElement;

        if (fe is ListBoxItem item && item.DataContext is string loc)
        {
            e.Handled = true;
            SelectLocation(loc);
        }
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!SuggestionsDropdown.IsVisible) return;
        var hit = e.OriginalSource as DependencyObject;
        if (!IsDescendantOf(hit, SearchBorder) && !IsDescendantOf(hit, SuggestionsDropdown))
        {
            HideDropdown();
            Focus();
        }
    }

    private void ShowDropdown()
    {
        var root = (FrameworkElement)Content;
        var pt   = SearchBorder.TranslatePoint(
            new Point(0, SearchBorder.ActualHeight + 2), root);
        SuggestionsDropdown.Margin   = new Thickness(pt.X, pt.Y, 0, 0);
        SuggestionsDropdown.MinWidth = SearchBorder.ActualWidth;
        SuggestionsDropdown.Visibility = Visibility.Visible;
    }

    private void HideDropdown() => SuggestionsDropdown.Visibility = Visibility.Collapsed;

    private void RefreshDropdown()
    {
        if (SearchBox.IsFocused && VM.Filtered.Count > 0)
            ShowDropdown();
        else if (!SuggestionsList.IsFocused)
            HideDropdown();
    }

    private void SearchIcon_Click(object sender, RoutedEventArgs e)
    {
        if (!_sidebarExpanded)
        {
            _sidebarExpanded = true;
            ApplySidebarState();
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => SearchBox.Focus());
        }
        else
        {
            SearchBox.Focus();
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
        => SelectLocation(SuggestionsList.SelectedItem as string
                       ?? (SuggestionsList.Items.Count > 0 ? SuggestionsList.Items[0] as string : null));

    private void SelectLocation(string? name)
    {
        HideDropdown();
        if (!string.IsNullOrWhiteSpace(name))
            VM.AddRouteCommand.Execute(name.Trim());
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject o)
    {
        if (o is ScrollViewer sv) return sv;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
        {
            var result = FindScrollViewer(VisualTreeHelper.GetChild(o, i));
            if (result is not null) return result;
        }
        return null;
    }

    // ── Title bar drag ──────────────────────────────────────────────────────────

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            MaximizeRestore_Click(sender, e);
        else
            DragMove();
    }

    // ── Window controls ─────────────────────────────────────────────────────────

    private void Minimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private static bool IsDescendantOf(DependencyObject? child, DependencyObject? ancestor)
    {
        var current = child;
        while (current is not null)
        {
            if (current == ancestor) return true;
            current = VisualTreeHelper.GetParent(current)
                   ?? LogicalTreeHelper.GetParent(current);
        }
        return false;
    }
}
