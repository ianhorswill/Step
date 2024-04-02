using Avalonia.Controls;

namespace AvaloniaRepl.Views;

public partial class MainWindow : Window
{
    private static MainWindow? _instance;

    public MainWindow()
    {
        InitializeComponent();
        _instance = this;
    }
    
    public static MainWindow Instance => _instance ?? new MainWindow();
    
    public object? GetActiveTabContent()
    {
        return Tabs.SelectedContent;
    }
    
    public void AddTab(string name, object content, bool select = true)
    {
        var tab = new TabItem
        {
            Header = name,
            Content = content
        };
        Tabs.Items.Add(tab);
        if (select) Tabs.SelectedItem = tab;
    }
    
    public T? FindTabByContentType<T>() where T : class
    {
        foreach (TabItem item in Tabs.Items)
        {
            if (item.Content is T tab)
            {
                return tab;
            }
        }
        return null;
    }
    
    public TabItem? FindTabByContent(object tab)
    {
        foreach (TabItem item in Tabs.Items)
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
}