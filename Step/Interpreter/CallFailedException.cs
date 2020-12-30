namespace Step.Interpreter
{
    /// <inheritdoc />
    public class CallFailedException : CallException
    {
        /// <summary>
        /// Indicates that a call to a Step task that shouldn't be able to fail did fail.
        /// </summary>
        /// <param name="task">Task called</param>
        /// <param name="arguments">Arguments</param>
        public CallFailedException(object task, object[] arguments)
        : base(task, arguments, $"Call failed: {Call.CallSourceText(task, arguments)}")
        { }
    }
}
