#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArgumentCountException.cs" company="Ian Horswill">
// Copyright (C) 2020 Ian Horswill
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion

namespace Step.Interpreter
{
    /// <summary>
    /// Signals a task was called with the wrong number of arguments
    /// </summary>
    public class ArgumentCountException : CallException
    {
        /// <inheritdoc />
        public ArgumentCountException(object task, int expected, object[] actual) 
            : base(task,actual, $"Wrong number of arguments for {task}, expected {expected}, got {actual.Length}: {Call.CallSourceText(task, actual)}")
        { }

        /// <summary>
        /// Check if the number of arguments is as expected.  If not, throw an exception.
        /// </summary>
        /// <param name="task">Name of task called (used in error message, if necessary)</param>
        /// <param name="expected">Number of arguments the task should take</param>
        /// <param name="arglist">Actual arguments passed</param>
        /// <exception cref="ArgumentCountException">When the number of arguments is incorrect.</exception>
        public static void Check(object task, int expected, object[] arglist)
        {
            if (expected != arglist.Length)
                throw new ArgumentCountException(task, expected, arglist);
        }

        /// <summary>
        /// Check if the number of arguments is as expected.  If not, throw an exception.
        /// </summary>
        /// <param name="task">Name of task called (used in error message, if necessary)</param>
        /// <param name="minArgs">Minimum number of arguments the task should take</param>
        /// <param name="arglist">Actual arguments passed</param>
        /// <exception cref="ArgumentCountException">When the number of arguments is incorrect.</exception>
        public static void CheckAtLeast(object task, int minArgs, object[] arglist)
        {
            if (arglist.Length < minArgs)
                throw new ArgumentCountException(task, minArgs, arglist);
        }
    }
}
