#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LocalVariableName.cs" company="Ian Horswill">
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
    /// The formal variable name for a local variable.
    /// This is not the run-time local variable itself, which differs from call to call.
    /// To get the run-time variable for a specific call, use
    /// BindingEnvironment.Local[LocalVariableName.Index].  This will be a LogicVariable,
    /// which can be dereferenced through the BindingEnvironment's Unifications field to
    /// get the actual value of the variable.
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class LocalVariableName : IVariableName
    {
        /// <summary>
        /// Name of the variable.
        /// Different methods with variables with the same name have different LocalVariableName objects
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// Position in the method's stack frame (the Locals field of the BindingEnvironment) of the LogicVariable
        /// holding this variable's value.
        /// </summary>
        public readonly int Index;

        internal LocalVariableName(string name, int index)
        {
            Name = name;
            Index = index;
        }

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
