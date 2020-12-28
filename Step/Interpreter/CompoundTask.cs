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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Step.Utilities;

namespace Step.Interpreter
{
    /// <summary>
    /// Task implemented as a set of methods, each composed of a series of Steps (sub-tasks)
    /// Tasks defined by user code are CompoundTasks
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class CompoundTask
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
        public readonly List<Method> Methods = new List<Method>();

        internal IList<Method> EffectiveMethods => Shuffle ? (IList<Method>)Methods.Shuffle() : Methods;

        [Flags]
        internal enum TaskFlags
        {
            None = 0,
            Shuffle = 1,
            MultipleSolutions = 2,
            Fallible = 4
        }

        internal TaskFlags Flags;

        /// <summary>
        /// True if the methods of the task should be tried in random order
        /// </summary>
        public bool Shuffle => (Flags & TaskFlags.Shuffle) != 0;

        /// <summary>
        /// True if this task should only ever generate at most one output
        /// </summary>
        public bool Deterministic => (Flags & TaskFlags.MultipleSolutions) == 0;

        /// <summary>
        /// True if it's an error for this call not to succeed at least once
        /// </summary>
        public bool MustSucceed => (Flags & TaskFlags.Fallible) == 0;

        internal CompoundTask(string name, int argCount)
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
        /// <param name="newFlags">Additional flags to set for the task</param>
        internal void AddMethod(object[] argumentPattern, LocalVariableName[] localVariableNames, Step stepChain, TaskFlags newFlags,
            string path, int lineNumber)
        {
            Flags |= newFlags;
            Methods.Add(new Method(this, argumentPattern, localVariableNames, stepChain, path, lineNumber));
        }

        /// <inheritdoc />
        public override string ToString() => Name;

        /// <summary>
        /// Remove all defined methods for this task
        /// </summary>
        public void EraseMethods()
        {
            Methods.Clear();
        }
    }
}
