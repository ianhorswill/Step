#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundTask.cs" company="Ian Horswill">
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

using System.Collections.Generic;
using System.Diagnostics;

namespace Step.Interpreter
{
    /// <summary>
    /// Task implemented as a set of methods, each composed of a series of Steps (sub-tasks)
    /// Tasks defined by user code are CompoundTasks
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    internal class CompoundTask
    {
        /// <summary>
        /// Name, for debugging purposes
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// Number of arguments expected by the task
        /// </summary>
        public readonly int ArgCount;
        /// <summary>
        /// Methods for accomplishing the task
        /// </summary>
        internal readonly List<Method> Methods = new List<Method>();

        public CompoundTask(string name, int argCount)
        {
            Name = name;
            ArgCount = argCount;
        }

        /// <summary>
        /// Add a new method for achieving this task
        /// </summary>
        /// <param name="argumentPattern">Terms (variables or values) to unify with the arguments in a call to test whether this method is appropriate</param>
        /// <param name="localVariableNames">LocalVariables used in this method</param>
        /// <param name="stepChain">Linked list of Step objects to attempt to execute when running this method</param>
        /// <param name="path">File from which the method was read</param>
        /// <param name="lineNumber">Line number where the method starts in the file</param>
        public void AddMethod(object[] argumentPattern, LocalVariableName[] localVariableNames, Step stepChain, string path, int lineNumber) 
            => Methods.Add(new Method(this, argumentPattern, localVariableNames, stepChain, path, lineNumber));

        public override string ToString() => Name;
    }
}
