using System;
using System.Collections.Generic;
using System.Text;

namespace Step
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
