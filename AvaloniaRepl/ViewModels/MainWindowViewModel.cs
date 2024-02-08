using System.Collections.Generic;

namespace AvaloniaRepl.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
#pragma warning disable CA1822 // Mark members as static
    public string Greeting => "Welcome to Avalonia!";
    public List<string> RecentProjectPaths { get; set; } = [];
#pragma warning restore CA1822 // Mark members as static
}