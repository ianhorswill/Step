using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
        debugger.DebugPauseCallback = UpdateInterface;
        
        MainWindow.Instance.SetTabDisplayName(this, $"Debugger ({StepCode.ProjectName})");
    }

    private void UpdateInterface()
    {
        MethodInfo.Text = $"{debugger.LastResult_CalledMethod}\nArguments: {string.Join(", ", debugger.LastResult_Args)}";
        BindingEnvironment.Text = debugger.LastResult_Environment?.ToTermString();
        Output.Text = debugger.LastResult_Text;
    }
}