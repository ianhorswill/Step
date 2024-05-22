using System.Collections.ObjectModel;
using StepRepl.Views;

namespace StepRepl.ViewModels;

public class TabViewModel : ViewModelBase
{
    public ObservableCollection<TabInfo> Tabs { get; set; } = new();
}