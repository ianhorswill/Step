using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Platform.Storage;
using AvaloniaRepl.ViewModels;

namespace AvaloniaRepl.Views;

public partial class MainWindow : Window
{
    
    public MainWindow()
    {
        InitializeComponent();
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
        // get the recent projects propery
        var recentProjects = (DataContext as MainWindowViewModel)?.RecentProjectPaths;
        if (recentProjects == null) return;
        
        // remove the path if it already exists
        if (recentProjects.Contains(path))
        {
            recentProjects.Remove(path);
        }
        
        // add the path to the top of the list
        
        recentProjects.Insert(0, path);
        
        // remove the last item if the list is too long
        
        while (recentProjects.Count > 5)
        {
            recentProjects.RemoveAt(recentProjects.Count - 1);
        }
        
        // update the UI
        
        


    }
    
    private void ShowWarningsAndException()
    {
        if (StepCode.LastException == null) return;
        
    }
}