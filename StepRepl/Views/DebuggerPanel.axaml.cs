﻿using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Step;
using Step.Interpreter;
using Step.Output;

namespace StepRepl.Views;

public partial class DebuggerPanel : UserControl
{
    private ReplDebugger _debugger;
    public DebuggerPanel()
    {
        InitializeComponent();
        this.Loaded += (s, e) =>
        {
            UpdateInterface();
        };
        this.DetachedFromVisualTree += (s, e) =>
        {
            _debugger?.End();
        };
    }
    
    public void SetDebugger(ReplDebugger debugger)
    {
        _debugger = debugger;
        UpdateInterface();
    }
    
    private void ContinueButtonPressed(object sender, RoutedEventArgs e)
    {
        _debugger.SingleStepping = false;
        _debugger?.Continue();
    }
    
    private void AbortButtonPressed(object? sender, RoutedEventArgs e)
    {
        StepCode.AbortCurrentStepThread();
    }
    
    private void SingleStepButtonPressed(object sender, RoutedEventArgs e)
    {
        _debugger.SingleStepping = true;
        _debugger.Continue();
    }

    private void UpdateInterface()
    {
        if (_debugger?.IsPaused ?? false)
        {
            CallField.Inlines.Clear();;
            CallField.Inlines.Add(HtmlTextFormatter.ParseHtml(MethodCallFrame.CurrentFrame.CallSourceTextWithCurrentBindings));
            OutputArea.IsVisible = true;
            DebugHint.IsVisible = false;
            MethodInfo.Inlines.Clear();
            MethodInfo.Inlines.Add(HtmlTextFormatter.ParseHtml($"{_debugger.LastResult_TraceEvent}: {_debugger.LastResult_Environment.Value.Frame.MethodSource}"));
            Output.Text = _debugger.LastResult_Text;
            if (_debugger.LastResult_Environment.HasValue)
            {
                StackTrace.SelectedItem = null;
                StackTrace.ItemsSource = _debugger.LastResult_Environment.Value.Frame.CallerChain;
            }
        }
        else
        {
            OutputArea.IsVisible = false;
            DebugHint.IsVisible = true;
        }
    }
    
    private void StackFrameGotFocus(object? sender, GotFocusEventArgs e)
    {
        var t = (SelectableTextBlock)sender;
        StackTrace.SelectedItem = t.DataContext;
    }
    
    private void ShowStackFrame(object? sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender;
        var frame = (MethodCallFrame)item.DataContext;
        var window = new MethodCallFrameViewer() { DataContext = frame };
        window.Show();
    }
    
    private void StackFrameSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (StackTrace.SelectedItem is MethodCallFrame { Method.FilePath: not null } frame)
        {
            VSCode.Edit(frame.Method.FilePath, frame.Method.LineNumber);
        }
    }
    
}