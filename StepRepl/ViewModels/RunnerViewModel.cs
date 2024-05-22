using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using StepRepl.Views;

namespace StepRepl.ViewModels;

public class RunnerViewModel : ViewModelBase, INotifyPropertyChanged
{
#pragma warning disable CA1822 // Mark members as static
    public ObservableCollection<string> RecentProjectPaths { get; set; } = [];
    public ObservableCollection<string> CommandHistory { get; set; } = [];
    public ObservableCollection<StepButton> StepButtons { get; set; } = new();
    
    private bool _evalWithDebugging;

    public bool EvalWithDebugging
    {
        get => _evalWithDebugging;
        set
        {
            _evalWithDebugging = value;
            OnPropertyChanged(nameof(EvalWithDebugging));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void AddCommandHistory(string cmd)
    {
        CommandHistory.Remove(cmd);
        
        CommandHistory.Insert(0, cmd);
        if (CommandHistory.Count > 16) CommandHistory.RemoveAt(CommandHistory.Count -1);
    }

    public void AddRecentProjects(string path)
    {
        RecentProjectPaths.Remove(path);

        RecentProjectPaths.Insert(0, path);
        if (RecentProjectPaths.Count > 10) RecentProjectPaths.RemoveAt(RecentProjectPaths.Count -1);
    }
#pragma warning restore CA1822 // Mark members as static
}