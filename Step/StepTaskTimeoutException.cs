using System;
using System.Collections.Generic;
using System.Text;

namespace Step
{
    /// <summary>
    /// Signal that a call to step code ran for too long without completing.
    /// </summary>
    public class StepTaskTimeoutException : TimeoutException
    {
        public StepTaskTimeoutException() : base("A call to Step code ran for too long without completing.")
        { }
    }
}
