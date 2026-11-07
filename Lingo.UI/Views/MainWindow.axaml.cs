using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Lingo.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnMenuExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}