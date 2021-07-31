using System.Diagnostics;

namespace Step.Interpreter
{
    /// <summary>
    /// Base class for objects representing tasks (things user code can call)
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public abstract class Task
    {
        /// <summary>
        /// Name, for debugging purposes
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Number of arguments required by this task, if fixed.
        /// </summary>
        public readonly int? ArgumentCount;

        /// <summary>
        /// Initialize name of task
        /// </summary>
        protected Task(string name, int? argumentCount)
        {
            Name = name;
            ArgumentCount = argumentCount;
        }

        /// <summary>
        /// Call this task with the specified arguments
        /// </summary>
        /// <param name="arglist">Task arguments</param>
        /// <param name="output">Output accumulated so far</param>
        /// <param name="env">Binding environment</param>
        /// <param name="predecessor">Most recently succeeded MethodCallFrame</param>
        /// <param name="k">Continuation</param>
        /// <returns>True if task succeeded and continuation succeeded</returns>
        /// <exception cref="CallFailedException">If the task fails</exception>
        public abstract bool Call(object[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame predecessor, Step.Continuation k);

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
