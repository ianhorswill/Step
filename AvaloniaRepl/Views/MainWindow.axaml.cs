using Avalonia.Controls;

namespace AvaloniaRepl.Views;

public partial class MainWindow : Window
{
    public static MainWindow? Instance;

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
    }
    
    public object? GetActiveTab()
    {
        return Tabs.SelectedContent;
    }
}