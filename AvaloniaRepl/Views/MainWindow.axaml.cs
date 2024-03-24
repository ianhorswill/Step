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

    private void ShowStackFrame(object? sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender;
        var frame = (MethodCallFrame)item.DataContext;
        var window = new MethodCallFrameViewer() { DataContext = frame };
        window.Show();
    }

    private void TestGraph_Click(object? sender, RoutedEventArgs e)
    {
        new GraphVisualization().Show();
    }
}