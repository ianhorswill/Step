#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArgumentInstantiationException.cs" company="Ian Horswill">
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
    /// Signals a task was called with an arg bound/unbound when it shouldn't have been
    /// </summary>
    public class ArgumentInstantiationException : CallException
    {
        /// <inheritdoc />
        public ArgumentInstantiationException(object task, BindingEnvironment e, object?[] args) 
            : base(task, args, $"Arguments to {task} incorrectly instantiated: {Call.CallSourceText(task, e.ResolveList(args))}")
        { }

        /// <inheritdoc />
        public ArgumentInstantiationException(object task, BindingEnvironment e, object?[] args, string additionalMessage)
            : base(task, args, $"Arguments to {task} incorrectly instantiated: {Call.CallSourceText(task, e.ResolveList(args))}.  {additionalMessage}")
        { }

        /// <summary>
        /// Check argument and throw instantiation exception if necessary.
        /// </summary>
        public static void Check(object task, object? arg, bool shouldBeInstantiated, BindingEnvironment e,
            object?[] args)
        {
            if (!(e.Resolve(arg) is LogicVariable) != shouldBeInstantiated)
                throw new ArgumentInstantiationException(task, e, args);
        }
    }
}
