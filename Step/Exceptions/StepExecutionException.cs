using Step.Output;
using System;

namespace Step.Exceptions
{
    /// <summary>
    /// Signals that something went wrong in a call to a task
    /// </summary>
    public class StepExecutionException : Exception
    {
        /// <summary>
        /// The task the program attempted to call
        /// </summary>
        public readonly TextBuffer Output;

        public readonly bool SuppressStackTrace;

        /// <summary>
        /// Signal that some problem occurred with the call to a task
        /// </summary>
        /// <param name="task">The task that was called</param>
        /// <param name="arguments">Its arguments</param>
        /// <param name="message">Message to print</param>
        /// <param name="output">Output so far</param>
        public StepExecutionException(string message, TextBuffer output, bool suppressStackTrace = false) : base(message)
        {
            Output = output;
            SuppressStackTrace = suppressStackTrace;
        }
    }
}
