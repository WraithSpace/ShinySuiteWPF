using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ShinySuite.Views;

public partial class MiniPlayerWindow : Window
{
    private const double BaseWidth = 200.0;

    public static readonly DependencyProperty ScaleProperty =
        DependencyProperty.Register(nameof(Scale), typeof(double), typeof(MiniPlayerWindow),
            new PropertyMetadata(1.0));

    public double Scale
    {
        get => (double)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public MiniPlayerWindow()
    {
        InitializeComponent();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Scale = ActualWidth / BaseWidth;
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Close_Click(object sender, MouseButtonEventArgs e)
        => Hide();

    private void RightEdge_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var newW = Width + e.HorizontalChange * Scale;
        if (newW >= MinWidth) Width = newW;
    }
}
