using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Lingo.UI.Services;
using Lingo.UI.ViewModels;
using Lingo.UI.Views;

namespace Lingo.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            var fileDialogService = new AvaloniaFileDialogService(() => desktop.MainWindow ?? mainWindow);
            var dialogService = new AvaloniaDialogService(() => desktop.MainWindow ?? mainWindow);
            mainWindow.DataContext = new MainViewModel(fileDialogService, dialogService);
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}