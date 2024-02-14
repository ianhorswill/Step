using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AvaloniaRepl.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
#pragma warning disable CA1822 // Mark members as static
    public ObservableCollection<string> RecentProjectPaths { get; set; } = [];
    
    
#pragma warning restore CA1822 // Mark members as static
}