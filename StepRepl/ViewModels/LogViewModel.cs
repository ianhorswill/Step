using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using Step.ReplSupport;

namespace StepRepl.ViewModels;

public class LogViewModel : INotifyPropertyChanged, IDisposable
{
    public static LogViewModel Singleton = new();

    private LogViewModel()
    {
        LogEvent.EventLogged += EventLogged;
    }

    public List<LogEvent> Events { get; } = new();

    private ConcurrentQueue<LogEvent> recentlyAdded = new();

    public void Clear()
    {
        Events.Clear();
        OnPropertyChanged(nameof(Events));
    }

    private void EventLogged(LogEvent e)
    {
        Dispatcher.UIThread.Post(() => AddEvent(e));
    }

    private void AddEvent(LogEvent e)
    {
        Events.Add(e);
        OnPropertyChanged(nameof(Events));
    }

    public void Dispose()
    {
        LogEvent.EventLogged -= EventLogged;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}