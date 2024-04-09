﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using AvaloniaRepl.Views;

namespace AvaloniaRepl.ViewModels;

public class RunnerViewModel : ViewModelBase
{
#pragma warning disable CA1822 // Mark members as static
    public ObservableCollection<string> RecentProjectPaths { get; set; } = [];
    public ObservableCollection<string> CommandHistory { get; set; } = [];
    public ObservableCollection<TabInfo> Tabs { get; set; } = new();

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