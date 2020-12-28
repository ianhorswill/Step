#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogicVariable.cs" company="Ian Horswill">
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

using System.Diagnostics;

namespace Step.Interpreter
{
    /// <summary>
    /// A variable that can be aliased to values and/or other variables using BindingEnvironment.Unify.
    /// It works like logic variables in any other programming language that has them, although they're
    /// implemented using deep binding through the Unifications list in the BindingEnvironment, rather than
    /// shallow binding with a trail (i.e. an undo stack).
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerName) + "}")]
    public class LogicVariable
    {
        /// <summary>
        /// Name of the variable.
        /// This is for debugging purposes only.  It has no functional role.
        /// </summary>
        public readonly LocalVariableName Name;

        /// <inheritdoc />
        public LogicVariable(LocalVariableName name)
        {
            Name = name;
        }

        /// <summary>
        /// The name as it should appear in the debugger.
        /// This name has the UID appended rather than just the raw Name field
        /// so that different variables with the same Name can be distinguished.
        /// </summary>
        public string DebuggerName => ToString();

#if DEBUG
        private static int uidCounter;
        /// <summary>
        /// A unique counter distinguishing this LogicVariable from all others
        /// </summary>
        internal readonly int Uid = uidCounter++;
        /// <inheritdoc />
        public override string ToString() => Name.Name + Uid;
#else
        /// <inheritdoc />
        public override string ToString() => Name.Name;
#endif
    }
}
