using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Step;
using Step.Output;

namespace AvaloniaRepl.Views;

public partial class DebuggerPage : UserControl
{
    private ReplDebugger debugger;
    public DebuggerPage()
    {
        InitializeComponent();
    }
    
    public void SetThreadForDebugger(StepThread thread)
    {
        debugger = new ReplDebugger(thread.Debugger);
        //debugger.DebugPauseCallback = UpdateInterface;
        debugger.OnDebugPauseCallback += UpdateInterface;
        Unloaded += (s, e) =>
        {
            debugger?.End();
        };
        
        MainWindow.Instance.SetTabDisplayName(this, $"Debugger ({StepCode.ProjectName})");
    }
    
    private void ContinueButtonPressed(object sender, RoutedEventArgs e)
    {
        debugger.Continue();
    }
    
    private void SingleStepButtonPressed(object sender, RoutedEventArgs e)
    {
        var button = (ToggleButton)sender;
        debugger.ToggleSingleStepping(button.IsChecked ?? false);
    }

    private void UpdateInterface()
    {
        MethodInfo.Text = $"{debugger.LastResult_CalledMethod}\nArguments: {string.Join(", ", debugger.LastResult_Args ?? Array.Empty<object>())}";
        BindingEnvironment.Text = debugger.LastResult_Environment?.ToTermString();
        Output.Text = debugger.LastResult_Text;
    }
    
}