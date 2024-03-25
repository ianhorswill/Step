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
    
    public object? GetActiveTab()
    {
        return Tabs.SelectedContent;
    }
}