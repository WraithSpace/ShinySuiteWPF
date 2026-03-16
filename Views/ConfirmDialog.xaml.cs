using System.Windows;

namespace ShinySuite.Views;

public partial class ConfirmDialog : Window
{
    public bool DontAskAgain => DontAskBox.IsChecked == true;

    public ConfirmDialog(string title, string message, bool showDontAskAgain = false)
    {
        InitializeComponent();
        DarkTitleBar.Apply(this);
        TitleBlock.Text   = title;
        MessageBlock.Text = message;
        if (showDontAskAgain)
            DontAskBox.Visibility = Visibility.Visible;
    }

    private void Yes_Click(object sender, RoutedEventArgs e) { DialogResult = true;  Close(); }
    private void No_Click (object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
