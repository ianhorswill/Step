using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Step.Interpreter;

namespace Step
{
    /// <summary>
    /// Represents a call to Step code to be executed in a separate thread.
    /// IMPORTANT: Step is not thread-safe; you can only have one StepTask running at a time.
    /// Step is run in a separate thread both for isolation reasons (an infinite recursion won't
    /// take down the app), and so that the main thread can single-step the Step program or otherwise
    /// inspect its stack.
    /// </summary>
    public class StepThread : IDisposable
    {
        /// <summary>
        /// Make a new thread to run a Step task.
        /// </summary>
        /// <param name="m">Module containing Step code</param>
        /// <param name="code">Call to execute</param>
        /// <param name="state">Initial state for the call</param>
        public StepThread(Module m, string code, State? state=null)
            : this(m, () => m.ParseAndExecute(code, state??State.Empty))
        {
        }

        /// <summary>
        /// Make a new thread to run a Step task.
        /// </summary>
        /// <param name="m">Module containing the task</param>
        /// <param name="state">Initial State in which to run the code.  Use State.Empty if there's no prior state.</param>
        /// <param name="taskName">Name of the task</param>
        /// <param name="args">Arguments to the task</param>
        public StepThread(Module m, State state, string taskName, params object[] args)
            : this(m, () => m.Call(state, taskName, args))
        {
        }

        /// <summary>
        /// Make a new thread to run a Step task.
        /// </summary>
        /// <param name="m">Module containing the task</param>
        /// <param name="state">Initial State in which to run the code.  Use State.Empty if there's no prior state.</param>
        /// <param name="task">Task to call</param>
        /// <param name="args">Arguments to the task</param>
        public StepThread(Module m, State state, Task task, params object[] args)
            : this(m, () => m.Call(state, task, args))
        {
        }

        /// <summary>
        /// Start a new job running
        /// </summary>
        /// <param name="m">Module containing Step code</param>
        /// <param name="start">Method to invoke the Step interpreter</param>
        public StepThread(Module? m, StepInvocation start)
        {
            if (Current is { IsCompleted: false })
                throw new InvalidOperationException("Only one StepThread can be supported at one time");
            MethodCallFrame.MaxStackDepth = 1000;
            Module = m ?? Module.Global;
            Module.Trace = null;
            this.start = start;
            Current = this;
        }

        internal string DebugState
        {
            get
            {
                var b = new StringBuilder();
                if (Thread != null)
                    b.AppendLine($"Thread: {Thread.ThreadState}");
                else
                    b.AppendLine("Thread: null");
                b.AppendLine($"Awaiter: {awaiter?.DebugState}");
                b.AppendLine(Debugger?.DebugState);

                return b.ToString();
            }
        }

        private readonly StepInvocation start;
        internal Thread? Thread;

        public StepThread Start()
        {
            if (Thread != null)
                throw new InvalidOperationException("StepThread already started");
            SpawnThreadIfNeeded();
            return this;
        }

        internal void SpawnThreadIfNeeded()
        {
            if (Thread != null) return;
            Thread = new Thread(() =>
                {
                    try
                    {
                        var (output, newState) = start();
                        Text = output;
                        if (debugger != null)
                            debugger.State = newState;
                    }
                    catch (Exception e)
                    {
                        Exception = e;
                        Text = null;
                    }

                    lock (this) // paranoia
                    {
                        IsCompleted = true;
                        ContinueAwaiter();
                        debugger?.ThreadTerminated();
                    }

                    Current = null;
                },
                8000000);
            Thread.Start();
        }

        private StepThreadDebugger? debugger;

        public StepThreadDebugger Debugger => debugger ??= new StepThreadDebugger(this);

        /// <summary>
        /// Packages a call to the Step interpreter
        /// </summary>
        /// <returns>Text output and final State of the interpreter</returns>
        public delegate (string? output, State outState) StepInvocation();

        /// <summary>
        /// Module from which the job is run
        /// </summary>
        public readonly Module Module;

        /// <summary>
        /// Stop the job as soon as it returns to the Step interpreter.
        /// </summary>
        public void Abort()
        {
            Module.SearchLimit = 1;
        }

        /// <summary>
        /// True if the job has finished running
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// The currently StepTask, if any.
        /// </summary>
        public static StepThread? Current;

        /// <summary>
        /// Text output from the job
        /// </summary>
        public string? Text;

        public State? FinalState;

        /// <summary>
        /// Exception thrown by the job, if any
        /// </summary>
        public Exception? Exception { get; private set; }

        #region Async support

        public StepThreadAwaiter GetAwaiter()
        {
            if (Thread == null)
                throw new InvalidOperationException("Attempting to await an unstarted StepThread");
            lock (this)
            {
                if (awaiter == null)
                    awaiter = new StepThreadAwaiter(this);
                return awaiter;
            }
        }

        private StepThreadAwaiter? awaiter;

        private void ContinueAwaiter()
        {
            lock (this)
            {
                awaiter?.Done();
                awaiter = null;
            }
        }

        public class StepThreadAwaiter : INotifyCompletion
        {
            public readonly StepThread StepThread;

            public bool IsCompleted => StepThread.IsCompleted;

            public void OnCompleted(Action continuation)
            {
                var completedAlready = false;
                lock (StepThread)
                {
                    if (IsCompleted)
                        completedAlready = true;
                    else
                        CompletionAction += continuation;
                }

                if (completedAlready)
                    continuation();
            }

            internal string DebugState =>
                $"IsCompleted: {IsCompleted}, CompletionAction: {CompletionAction?.ToString()}";

            public (string? text, State? finalState) GetResult()
            {
                if (StepThread.Exception != null)
                    throw StepThread.Exception;
                return (StepThread.Text, StepThread.FinalState);
            }

            internal StepThreadAwaiter(StepThread stepThread)
            {
                StepThread = stepThread;
            }

            // Precondition: called with StepThread locked
            internal void Done()
            {
                if (CompletionAction != null)
                    CompletionAction();
            }

            private event Action? CompletionAction;
        }
        #endregion

        #region Debugger support
        public class StepThreadDebugger : IDisposable
        {
            internal StepThreadDebugger(StepThread stepThread)
            {
                StepThread = stepThread;
                continueEvent = new AutoResetEvent(false);
            }

            public readonly StepThread StepThread;

            internal string DebugState
            {
                get
                {
                    var b = new StringBuilder();
                    b.AppendLine($"IsPaused: {IsPaused}");
                    b.AppendLine($"Last TraceEvent: {TraceEvent}");
                    if (awaiter == null)
                        b.AppendLine("Debugger awaiter: null");
                    else
                        b.AppendLine($"Debugger awaiter: {awaiter.DebugState}");
                    if (Environment.HasValue)
                        b.AppendLine(Step.Module.StackTrace(Environment.Value.Unifications));
                    return b.ToString();
                }
            }

            /// <summary>
            /// True if the job is not running (e.g. it's at a breakpoint)
            /// </summary>
            public bool IsPaused { get; private set;  }

            /// <summary>
            /// Continue execution of a paused job.
            /// </summary>
            public StepThreadDebugger Continue()
            {
                if (!IsPaused)
                    throw new InvalidOperationException("Attempt to continue a StepTask that isn't paused");
                continueEvent.Set();
                return this;
            }

            public void Start() => StepThread.SpawnThreadIfNeeded();

            /// <summary>
            /// Binding environment of the job, when it was last paused.
            /// </summary>
            public BindingEnvironment? Environment { get; private set; }

            /// <summary>
            /// State of the job when it was last paused or completed
            /// </summary>
            public State State { get; set; }

            /// <summary>
            /// TraceEvent that paused the job, if any
            /// </summary>
            public Module.MethodTraceEvent TraceEvent { get; private set; }

            public Method? CalledMethod;

            public object?[]? MethodArgs;

            /// <summary>
            /// If true, and if the job is running within an IDE, the IDE should show the Step stack.
            /// </summary>
            public bool ShowStackRequested;

            /// <summary>
            /// If true, the task should run for one step and then pause again
            /// </summary>
            public bool SingleStep
            {
                get => StepThread.Module.Trace != null;
                set
                {
                    if (value != SingleStep)
                        StepThread.Module.Trace = value ? SingleStepTraceHandler : (Module.TraceHandler?)null;
                }
            }

            /// <summary>
            /// If non-null, and if single-stepping, then job should not stop until returning from this frame.
            /// </summary>
            public MethodCallFrame? StepOverFrame;

            
            /// <summary>
            /// Event used to coordinate starts/stops
            /// </summary>
            private readonly AutoResetEvent continueEvent;

            private void SingleStepTraceHandler(Module.MethodTraceEvent e, Method method, object?[] args, TextBuffer output,
                BindingEnvironment env)
            {
                if (StepOverFrame == null
                    || (StepOverFrame.StackDepth >= MethodCallFrame.CurrentFrame!.Caller!.StackDepth &&
                        (e == Module.MethodTraceEvent.Succeed || e == Module.MethodTraceEvent.CallFail)))
                {
                    StepThread.Text = output.AsString;
                    State = env.State;
                    CalledMethod = method;
                    MethodArgs = args;
                    Environment = env;
                    Pause(e, true);
                    TraceEvent = Module.MethodTraceEvent.None;
                    CalledMethod = null;
                    MethodArgs = null;
                    Environment = null;
                }
            }

            /// <summary>
            /// Pause the job
            /// </summary>
            /// <param name="e">Event causing the task to pause</param>
            /// <param name="showStack">Tell the controlling thread that the user should see a stack dump</param>
            private void Pause(Module.MethodTraceEvent e, bool showStack = false)
            {
                // BUG: DebuggerBurnIn test fails in Run mode if this is removed or placed anywhere after ContinueAwaiter call
                Thread.Sleep(1);
                ShowStackRequested = showStack;
                IsPaused = true;
                TraceEvent = e;
                ContinueAwaiter(); // This will lock the StepThread
                continueEvent.WaitOne();
                IsPaused = false;
            }

            public void Dispose() => continueEvent.Dispose();

            public DebuggerAwaiter GetAwaiter()
            {
                if (awaiter != null)
                    throw new InvalidOperationException(
                        "Getting debugger awaiter when an existing awaiter is uncompleted");
                lock (StepThread)  // probably unnecessary
                {
                    return awaiter = new DebuggerAwaiter(this);
                }
            }

            private DebuggerAwaiter? awaiter;

            private void ContinueAwaiter()
            {
                lock (StepThread)
                {
                    var a = awaiter;
                    awaiter = null;
                    a?.Done();
                }
            }

            public class DebuggerAwaiter : INotifyCompletion
            {
                public readonly StepThreadDebugger Debugger;

                public bool IsCompleted => Debugger.StepThread.IsCompleted || awaiterCompleted;
                private bool awaiterCompleted;

                public void OnCompleted(Action continuation)
                {
                    var completedAlready = false;
                    lock (Debugger.StepThread)
                    {
                        if (completionAction != null)
                            throw new InvalidOperationException(
                                "StepThread debugger does not support multiple awaiters");
                        if (awaiterCompleted)
                            completedAlready = true;
                        else
                            completionAction = continuation;
                    }

                    if (completedAlready)
                        continuation();
                }

                public (Module.MethodTraceEvent TraceEvent, Method? CalledMethod, object?[]? args, string? Text, BindingEnvironment? Environment) GetResult()
                {
                    if (Debugger.StepThread.Exception != null)
                        throw Debugger.StepThread.Exception;
                    return (Debugger.TraceEvent, Debugger.CalledMethod, Debugger.MethodArgs, Debugger.StepThread.Text, Debugger.Environment);
                }

                internal DebuggerAwaiter(StepThreadDebugger d)
                {
                    Debugger = d;
                }

                // Precondition: called with StepThread locked
                internal void Done()
                {
                    if (awaiterCompleted)
                        throw new InvalidOperationException("Debugger awaiter called completed twice");
                    awaiterCompleted = true;
                    completionAction?.Invoke();
                }

                private Action? completionAction;

                internal string DebugState =>
                    $"ThreadIsCompleted: {IsCompleted}, awaiter completed: {awaiterCompleted}, CompletionAction: {completionAction?.ToString()}";
            }

            // Precondition: called with StepThread locked
            public void ThreadTerminated()
            {
                TraceEvent = Module.MethodTraceEvent.None;
                awaiter?.Done();
                awaiter = null;
            }
        }


        #endregion

        public void Dispose()
        {
            debugger?.Dispose();
        }
    }
}
