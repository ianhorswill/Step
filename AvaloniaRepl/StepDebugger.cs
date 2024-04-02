using System;
using Step;
using Step.Interpreter;
using Step.Output;

namespace AvaloniaRepl;

public class ReplDebugger
{
    private readonly StepThread.StepThreadDebugger _debugger;
    private StepThread.StepThreadDebugger.DebuggerAwaiter _awaiter;
    private (Module.MethodTraceEvent TraceEvent, Method? CalledMethod, object?[]? args, string? Text, BindingEnvironment? Environment) _lastResult;
    public Action? DebugPauseCallback;
    
    public Module.MethodTraceEvent LastResult_TraceEvent => _lastResult.TraceEvent;
    public Method? LastResult_CalledMethod => _lastResult.CalledMethod;
    public object?[]? LastResult_Args => _lastResult.args;
    public string? LastResult_Text => _lastResult.Text;
    public BindingEnvironment? LastResult_Environment => _lastResult.Environment;
    
    public ReplDebugger(StepThread.StepThreadDebugger debugger)
    {
        _debugger = debugger;
        _debugger.ShowStackRequested = true;
        EstablishAwaiter();
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
            EstablishAwaiter();
    }

    /// <summary>
    /// Sets up an Awaiter which allows us to catch the debugger's breaks.
    /// Must be reset after each break.
    /// </summary>
    private void EstablishAwaiter()
    {
        _awaiter = _debugger.GetAwaiter();
        _awaiter.OnCompleted(DebugBreakReady);
    }

    // Grab the Step execution information from the Awaiter
    private void DebugBreakReady()
    {
        _lastResult = _awaiter.GetResult();
        DebugPauseCallback?.Invoke();
    }
    
    /// <summary>
    /// Ends a debugging session.
    /// </summary>
    public void End()
    {
        _debugger.Dispose();
    }
}