using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using StepRepl.ViewModels;
using StepRepl.Views;

namespace StepRepl;

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
            // save user prefs on shutdown
            desktop.Exit += (sender, args) => Preferences.SaveToDisk();
            
            desktop.MainWindow = new MainWindow
            {
                DataContext = new TabViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}