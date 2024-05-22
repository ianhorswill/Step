using System;
using Step;
using Step.Interpreter;
using Step.Output;
using Task = System.Threading.Tasks.Task;

namespace AvaloniaRepl;

public class ReplDebugger
{
    private readonly StepThread.StepThreadDebugger _debugger;
    private (Module.MethodTraceEvent TraceEvent, Method? CalledMethod, object?[]? args, string? Text, BindingEnvironment? Environment) _lastResult;
    public Action<ReplDebugger>? OnDebugPauseCallback;

    public bool SingleStepping
    {
        get => _debugger.SingleStep;
        set => _debugger.SingleStep = value;
    }
    
    public bool IsPaused => _debugger.IsPaused;
    
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
        Console.WriteLine($"Confirming a debug session started for project {StepCode.ProjectName}");
    }
    
    public void Continue()
    {
        try
        {
            _debugger.Continue();
            if (_debugger.SingleStep)
                Task.Run(EstablishAwaiter);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error while continuing debugger: {e.Message}");
        }
    }

    /// <summary>
    /// Await the next debugger break and update the last result.
    /// </summary>
    private async Task EstablishAwaiter()
    {
        var debugResult = await _debugger;
        _lastResult = (debugResult.TraceEvent, debugResult.CalledMethod, debugResult.args, debugResult.Text, debugResult.Environment);
        OnDebugPauseCallback?.Invoke(this);
    }
    
    /// <summary>
    /// Ends a debugging session.
    /// </summary>
    public void End()
    {
        // keeps the thread from getting stuck
        try
        {
            _debugger.SingleStep = false;
            _debugger?.Continue();

            _debugger?.Dispose();
        }
        catch (Exception e) // oh well...
        {
            // ignored
        }
    }
}