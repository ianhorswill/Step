using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvaloniaRepl.ViewModels;
using Step.Interpreter;
using Task = System.Threading.Tasks.Task;

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

        if (chosen.Count == 0 || !Directory.Exists(chosen[0].Path.LocalPath)) return;
        string path = chosen[0].Path.LocalPath;
        OpenProject(path);
    }

    private void OpenRecentProject(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;
        
        string? path = (string?)menuItem.Header;
        if (Directory.Exists(path))
        {
            OpenProject(path);
        }
    }

    private void OpenProject(string path)
    {
        StepCode.ProjectDirectory = path;
        StepCode.ReloadStepCode();
        UpdateRecentProjects(path);
        this.Title = $"{StepCode.ProjectName} - StepRepl";
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
        var warnings = StepCode.Module.WarningsWithOffenders().ToArray();
        var warningText = warnings.Select(info => info.Warning);
        var haveWarnings = warnings.Length > 0;
        WarningLabel.IsVisible = haveWarnings;
        WarningText.ItemsSource = haveWarnings ? warnings : null;

        UpdateExceptionInfo();
    }
    
    public IEnumerable<MethodCallFrame> StackFrames 
        => MethodCallFrame.CurrentFrame == null?Array.Empty<MethodCallFrame>():MethodCallFrame.CurrentFrame.CallerChain;
    
    private void UpdateExceptionInfo()
    {
        if (StepCode.LastException != null)
        {
            ErrorLabel.IsVisible = true;
            //Module.RichTextStackTraces = true;
            ExceptionMessage.Text = StepCode.LastException.Message;
            StackTrace.ItemsSource = StackFrames;
                
            CStackTrace.Text = "Internal debugging information for Ian:\n"+StepCode.LastException.StackTrace;
        }
        else
        {
            ExceptionMessage.Text = CStackTrace.Text = "";
            StackTrace.ItemsSource = null;
            ErrorLabel.IsVisible = false;
        }
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
        
    }
}