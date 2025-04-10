﻿using System;

namespace Step.Interpreter
{
    /// <summary>
    /// Signals that something went wrong in a call to a task
    /// </summary>
    public class CallException : StepExecutionException
    {
        /// <summary>
        /// The task the program attempted to call
        /// </summary>
        public readonly object Task;
        /// <summary>
        /// The arguments passed to the task
        /// </summary>
        public readonly object?[]? Arguments;

        /// <summary>
        /// Signal that some problem occurred with the call to a task
        /// </summary>
        /// <param name="task">The task that was called</param>
        /// <param name="arguments">Its arguments</param>
        /// <param name="message">Message to print</param>
        /// <param name="output">Output so far</param>
        public CallException(object task, object?[]? arguments, string message, TextBuffer output) : base(message, output)
        {
            Task = task;
            Arguments = arguments;
        }
    }
}
