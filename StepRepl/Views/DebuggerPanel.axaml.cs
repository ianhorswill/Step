#nullable enable
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Step.Interpreter;
using Step.Interpreter.Steps;
using Module = Step.Module;

namespace StepRepl.Views;

public partial class DebuggerPanel : UserControl
{
    private ReplDebugger debugger;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public DebuggerPanel()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        InitializeComponent();
        this.Loaded += (s, e) =>
        {
            UpdateInterface();
        };
        this.DetachedFromVisualTree += (s, e) =>
        {
            debugger?.End();
        };
    }
    
    public void SetDebugger(ReplDebugger debugger)
    {
        this.debugger = debugger;
        UpdateInterface();
    }
    
    private void ContinueButtonPressed(object sender, RoutedEventArgs e)
    {
        debugger.SingleStepping = false;
        debugger?.Continue();
    }
    
    private void AbortButtonPressed(object? sender, RoutedEventArgs e)
    {
        StepCode.AbortCurrentStepThread();
    }
    
    private void SingleStepButtonPressed(object sender, RoutedEventArgs e)
    {
        debugger.SingleStepping = true;
        debugger.Continue();
    }

    private void UpdateInterface()
    {
        if (debugger?.IsPaused ?? false)
        {
            CallField.Inlines!.Clear();;
            var source = Call.CallSourceText(
                MethodCallFrame.CurrentFrame!.Method!.Task,
                debugger.LastResult_Environment!.Value.ResolveList(MethodCallFrame.CurrentFrame.Arglist),
                Module.RichTextStackTraces, debugger.LastResult_Environment!.Value.Unifications);
            CallField.Inlines.Add(HtmlTextFormatter.ParseHtml(source));
            OutputArea.IsVisible = true;
            DebugHint.IsVisible = false;
            MethodInfo.Inlines!.Clear();
            MethodInfo.Inlines!.Add(HtmlTextFormatter.ParseHtml($"{debugger.LastResult_TraceEvent}: {debugger.LastResult_Environment.Value.Frame.MethodSource}"));
            Output.Text = debugger.LastResult_Text;
            if (debugger.LastResult_Environment.HasValue)
            {
                StackTrace.SelectedItem = null;
                StackTrace.ItemsSource = debugger.LastResult_Environment.Value.Frame.CallerChain;
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
        var t = (SelectableTextBlock)sender!;
        StackTrace.SelectedItem = t.DataContext;
    }
    
    private void ShowStackFrame(object? sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender!;
        var frame = (MethodCallFrame)item.DataContext!;
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