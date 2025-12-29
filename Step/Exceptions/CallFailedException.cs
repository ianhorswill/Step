using Step.Interpreter.Steps;
using Step.Output;

namespace Step.Exceptions
{
    /// <inheritdoc />
    public class CallFailedException : CallException
    {
        /// <summary>
        /// Indicates that a call to a Step task that shouldn't be able to fail did fail.
        /// </summary>
        /// <param name="task">Task called</param>
        /// <param name="arguments">Arguments</param>
        public CallFailedException(object? task, object?[] arguments, TextBuffer output)
        : base(task??"null", arguments, $"Call failed: {Call.CallSourceText(task??"null", arguments, Module.RichTextStackTraces)}", output)
        { }
    }
}
