using Step;
using Task = Step.Interpreter.Task;

namespace Repl
{
    /// <summary>
    /// A Step task call, closed over a State object
    /// </summary>
    public class StepClosure
    {
        public readonly Module Module;
        public readonly State State;
        public readonly Task Task;
        public readonly object[] Arguments;

        public StepClosure(Module module, State state, Task task, params object[] arguments)
        {
            State = state;
            Task = task;
            Module = module;
            Arguments = arguments;
        }

        public StepThread Invoke() => new StepThread(Module, State, Task, Arguments).Start();
    }
}
