using System;
using System.Threading;
using Step.Interpreter;

namespace Step
{
    /// <summary>
    /// Represents a call to Step code to be executed in a separate thread.
    /// IMPORTANT: Step is not thread-safe; you can only have one StepJob running at a time.
    /// Step is run in a separate thread both for isolation reasons (an infinite recursion won't
    /// take down the app), and so that the main thread can single-step the Step program or otherwise
    /// inspect its stack.
    /// </summary>
    public class StepJob
    {
        /// <summary>
        /// Start a new job running
        /// </summary>
        /// <param name="m">Module containing Step code</param>
        /// <param name="singleStep">Whether to begin the job in single-step mode</param>
        /// <param name="code">Call to execute</param>
        /// <param name="state">Initial state for the call</param>
        public StepJob(Module m, bool singleStep, string code, State state)
            : this(m, singleStep, () => m.ParseAndExecute(code, state))
        {
        }

        /// <summary>
        /// Start a new job running
        /// </summary>
        /// <param name="m">Module containing Step code</param>
        /// <param name="singleStep">Whether to being the job in single-step mode</param>
        /// <param name="start">Method to invoke the Step interpreter</param>
        public StepJob(Module? m, bool singleStep, StepInvocation start)
        {
            MethodCallFrame.MaxStackDepth = 1000;
            Module = m ?? Module.Global;
            SingleStep = singleStep;

            var thread = new Thread(() =>
            {
                Current = this;
                try
                {
                    var (output, newState) = start();
                    Text = output;
                    State = newState;
                }
                catch (Exception e)
                {
                    Exception = e;
                    Text = null;
                }

                IsCompleted = true;
            }, 8000000);
            thread.Start();
        }

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
        /// True if the job is not running (e.g. it's at a breakpoint)
        /// </summary>
        public bool IsPaused { get; private set;  }

        /// <summary>
        /// Continue execution of a paused job.
        /// </summary>
        public void Continue()
        {
            continueEvent.Set();
        }

        /// <summary>
        /// Stop the job as soon as it returns to the Step interpreter.
        /// </summary>
        public void Abort()
        {
            Module.SearchLimit = 0;
        }

        /// <summary>
        /// True if the job has finished running
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// The currently StepJob, if any.
        /// </summary>
        public static StepJob? Current;

        /// <summary>
        /// Text output from the job
        /// </summary>
        public string? Text;

        /// <summary>
        /// Binding environment of the job, when it was last paused.
        /// </summary>
        public BindingEnvironment Environment { get; private set; }

        /// <summary>
        /// State of the job when it was last paused or completed
        /// </summary>
        public State State { get; set; }

        /// <summary>
        /// Exception thrown by the job, if any
        /// </summary>
        public Exception? Exception { get; private set; }

        /// <summary>
        /// TraceEvent that paused the job, if any
        /// </summary>
        public Module.MethodTraceEvent TraceEvent { get; private set; }

        /// <summary>
        /// If true, and if the job is running within an IDE, the IDE should show the Step stack.
        /// </summary>
        public bool ShowStackRequested;

        /// <summary>
        /// If true, the task should run for one step and then pause again
        /// </summary>
        public bool SingleStep
        {
            get => Module.Trace != null;
            set
            {
                if (value != SingleStep)
                    Module.Trace = value ? SingleStepTraceHandler : (Module.TraceHandler?)null;
            }
        }

        /// <summary>
        /// If non-null, and if single-stepping, then job should not stop until returning from this frame.
        /// </summary>
        public MethodCallFrame? StepOverFrame;

        private void SingleStepTraceHandler(Module.MethodTraceEvent e, Method method, object?[] args, TextBuffer output,
            BindingEnvironment env)
        {
            if (StepOverFrame == null
                || (StepOverFrame.StackDepth >= MethodCallFrame.CurrentFrame!.Caller!.StackDepth &&
                    (e == Module.MethodTraceEvent.Succeed || e == Module.MethodTraceEvent.CallFail)))
            {
                TraceEvent = e;
                Text = output.AsString;
                State = env.State;
                Environment = env;
                Pause(true);
                TraceEvent = Module.MethodTraceEvent.None;
            }
        }

        /// <summary>
        /// Pause the job
        /// </summary>
        /// <param name="showStack">Tell the controlling thread that the user should see a stack dump</param>
        private void Pause(bool showStack = false)
        {
            ShowStackRequested = showStack;
            IsPaused = true;
            continueEvent.WaitOne();
            IsPaused = false;
        }

        /// <summary>
        /// Event used to coordinate starts/stops
        /// </summary>
        private readonly AutoResetEvent continueEvent = new AutoResetEvent(false);
    }
}
