using System.Windows;

namespace NyxLaunch.App;

public partial class ArgumentWindow : Window
{
    public string Arguments => ArgumentsBox.Text.Trim();
    public ArgumentWindow(string gameName, string arguments)
    {
        InitializeComponent(); GameTitle.Text = $"Launch options · {gameName}"; ArgumentsBox.Text = arguments;
    }
    private void CloseWindow_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void Save_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
