using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaRepl.ViewModels;

namespace AvaloniaRepl.Views;

public partial class MainWindow : Window
{

    public MainWindow()
    {
        InitializeComponent();
    }
    
    private void ReloadStepCode(object? sender, RoutedEventArgs e)
    {
        StepCode.ReloadStepCode();
        ((MainWindowViewModel) DataContext).StepOutput.Clear();
        StepOutput.Text = "";
        ShowWarningsAndException();
    }
    
    private void EditProject(object? sender, RoutedEventArgs e)
    {
        if (Directory.Exists(StepCode.ProjectDirectory))
        {
            VSCode.EditFolder(StepCode.ProjectDirectory);
        }
    }

    private async void SelectProjectFolder(object? sender, RoutedEventArgs e)
    {
        var chosen = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Select a project directory",
            SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(StepCode.ProjectDirectory)
        });

        if (chosen.Count == 0 || !Directory.Exists(chosen[0].Path.AbsolutePath)) return;
        string path = chosen[0].Path.AbsolutePath;
        OpenProject(path);
    }

    private void OpenRecentProject(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            string path = textBlock.Text;
            if (Directory.Exists(path))
            {
                OpenProject(path);
            }
        }
    }

    private void OpenProject(string path)
    {
        StepCode.ProjectDirectory = path;
        StepCode.ReloadStepCode();
        UpdateRecentProjects(path);
        ShowWarningsAndException();
    }

    private void Quit(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void UpdateRecentProjects(string path)
    {
        var recentProjects = ((MainWindowViewModel) DataContext).RecentProjectPaths;
        if (recentProjects.Contains(path))
        {
            recentProjects.Remove(path);
        }

        recentProjects.Insert(0, path);
        
        ((MainWindowViewModel) DataContext).RecentProjectPaths = recentProjects;
    }

    private void ShowWarningsAndException()
    {
        if (StepCode.LastException == null) return;



    }

    private async void StepCommandField_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return || e.Key != Key.Enter) return;
        if (sender is not TextBox textBox) return;

        string command = textBox.Text;
        textBox.Text = "";
        if (string.IsNullOrEmpty(command)) return;

        await EvalAndShowOutput(command);
    }

    Task EvalAndShowOutput(string command) => EvalAndShowOutput(StepCode.Eval(command));

    /// <summary>
    /// Run some step code and then update the page with its output and/or exceptions
    /// </summary>
    /// <param name="evalTask">Task that runs the step code and returns its output text</param>
    async Task EvalAndShowOutput(Task<string> evalTask)
    {
        // Update step output in view model
        var stepOutput = ((MainWindowViewModel) DataContext).StepOutput;
        var outputText = await evalTask;
        
        stepOutput.Add(outputText);
        StepOutput.Text = outputText;
    }
}