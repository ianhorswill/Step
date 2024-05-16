using System;
using Step;
using Step.Interpreter;
using Step.Output;
using Task = System.Threading.Tasks.Task;

namespace AvaloniaRepl;

public class ReplDebugger
{
    private readonly StepThread.StepThreadDebugger _debugger;
    private StepThread.StepThreadDebugger.DebuggerAwaiter _awaiter;
    private (Module.MethodTraceEvent TraceEvent, Method? CalledMethod, object?[]? args, string? Text, BindingEnvironment? Environment) _lastResult;
    public delegate void DebugPauseCallbackDelegate();
    public event DebugPauseCallbackDelegate? OnDebugPauseCallback;
    
    
    public Module.MethodTraceEvent LastResult_TraceEvent => _lastResult.TraceEvent;
    public Method? LastResult_CalledMethod => _lastResult.CalledMethod;
    public object?[]? LastResult_Args => _lastResult.args;
    public string? LastResult_Text => _lastResult.Text;
    public BindingEnvironment? LastResult_Environment => _lastResult.Environment;
    
    public ReplDebugger(StepThread.StepThreadDebugger debugger)
    {
        _debugger = debugger;
        _debugger.ShowStackRequested = true;
        Task.Run(EstablishAwaiter);
        _debugger.Start();
        Console.WriteLine($"Confirming a debug session started for project {StepCode.ProjectName}");
    }
    
    public void ToggleSingleStepping(bool singleStep)
    {
        _debugger.SingleStep = singleStep;
    }
    
    public void Continue(bool awaitNextBreak = true)
    {
        _debugger.Continue();
        if (awaitNextBreak)
            Task.Run(EstablishAwaiter);
    }

    /// <summary>
    /// Await the next debugger break and update the last result.
    /// </summary>
    private async Task EstablishAwaiter()
    {
        var debugResult = await _debugger;
        _lastResult = (debugResult.TraceEvent, debugResult.CalledMethod, debugResult.args, debugResult.Text, debugResult.Environment);
        OnDebugPauseCallback?.Invoke();
    }
    
    /// <summary>
    /// Ends a debugging session.
    /// </summary>
    public void End()
    {
        _debugger.Dispose();
    }
}