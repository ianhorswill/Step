using System;

namespace Step.Interpreter
{
    /// <inheritdoc />
    public class CallFailedException : Exception
    {
        /// <summary>
        /// The task that was called and failed
        /// </summary>
        public readonly object Task;
        /// <summary>
        /// The arguments to the task
        /// </summary>
        public readonly object[] Arguments;

        /// <summary>
        /// Indicates that a call to a Step task that shouldn't be able to fail did fail.
        /// </summary>
        /// <param name="task">Task called</param>
        /// <param name="arguments">Arguments</param>
        public CallFailedException(object task, object[] arguments)
        : base($"Call failed: {Call.CallSourceText(task, arguments)}")
        {
            Task = task;
            Arguments = arguments;
        }
    }
}
