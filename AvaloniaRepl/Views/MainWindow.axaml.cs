using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaRepl.ViewModels;

namespace AvaloniaRepl.Views;

public class TabInfo
{
    public string Header { get; set; }
    public UserControl Content { get; set; }
}

public partial class MainWindow : Window
{
    private static MainWindow? _instance;

    public MainWindow()
    {
        InitializeComponent();
        _instance = this;
        Loaded += (sender, args) =>
        {
            AddTab("Runner", new RunnerPage{DataContext = new RunnerViewModel()}, true);
        };
    }

    public static MainWindow Instance => _instance ?? new MainWindow();
    public TabViewModel ViewModel => (TabViewModel)Instance.DataContext;
    
    public object? GetActiveTabContent()
    {
        return TabView.SelectedContent;
    }
    
    public void AddTab(string name, UserControl content, bool select = true)
    {
        var tab = new TabInfo
        {
            Header = name,
            Content = content
        };
        
        MainWindow.Instance.ViewModel.Tabs.Add(tab);
        if (select) TabView.SelectedItem = tab;
    }
    
    public T? FindTabByContentType<T>() where T : class
    {
        foreach (TabInfo item in ViewModel.Tabs)
        {
            if (item.Content is T tab)
            {
                return tab;
            }
        }
        return null;
    }
    
    public TabInfo? FindTabByContent(object tab)
    {
        foreach (TabInfo item in ViewModel.Tabs)
        {
            if (item.Content == tab)
            {
                return item;
            }
        }
        return null;
    }
    
    public void SetTabDisplayName(object tabContent, string name)
    {
        var foundTab = FindTabByContent(tabContent);
        if (foundTab != null)
        {
            foundTab.Header = name;
        }
    }

    private void CloseTabClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not TabInfo tab) return;

        ViewModel.Tabs.Remove(tab);
        
        // if no tabs left, close the app
        if (ViewModel.Tabs.Count == 0)
        {
            Close();
        }
    }
}