using System;

namespace Step.Exceptions
{
    /// <summary>
    /// An exception thrown by a step thread
    /// </summary>
    public class StepException : Exception
    {
        internal StepException(Exception innerException) : base(innerException.Message, innerException)
        { }
    }
}
