using System.Collections.ObjectModel;
using AvaloniaRepl.Views;

namespace AvaloniaRepl.ViewModels;

public class TabViewModel : ViewModelBase
{
    public ObservableCollection<TabInfo> Tabs { get; set; } = new();
}