#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArgumentTypeException.cs" company="Ian Horswill">
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

namespace Step.Interpreter
{
    /// <summary>
    /// Signals a task was called with the wrong kind of argument
    /// </summary>
    public class ArgumentTypeException : CallException
    {
        /// <inheritdoc />
        public ArgumentTypeException(object task, Type expected, object? actual, object?[]? arglist, TextBuffer output) 
            : base(task, arglist, 
                arglist ==null?
                    $"Wrong argument type in call to {task}, expected {TypeName(expected)}, got {actual??"null"}"
                    : $"Wrong argument type in call to {task}, expected {TypeName(expected)}, got {actual??"null"} in {Call.CallSourceText(task, arglist, Module.RichTextStackTraces)}",
                output)
        { }

        public ArgumentTypeException(object task, string message, object?[] arglist, TextBuffer output) 
            : base(task, arglist, 
                $"{message} in {Call.CallSourceText(task, arglist, Module.RichTextStackTraces)}",
                output)
        { }

        private static string TypeName(Type t) => t == typeof(object[]) ? "tuple" : t.Name;

        /// <summary>
        /// Check the specified argument value is of the right type.  If not, throw exception
        /// </summary>
        /// <param name="task">Name of task - used in error message if necessary</param>
        /// <param name="expected">Type expected</param>
        /// <param name="actual">Value provided</param>
        /// <param name="arglist">Full argument list of the task</param>
        /// <param name="allowUninstantiated">If true, accept an unbound variable as a value</param>
        /// <exception cref="ArgumentTypeException">When value isn't of the expected type</exception>
        public static void Check(object task, Type expected, object? actual, object?[]? arglist, TextBuffer output, bool allowUninstantiated = false)
        {
            if (allowUninstantiated && actual is LogicVariable)
                return;
            if (actual == null || (!expected.IsInstanceOfType(actual) && !(expected == typeof(float) && actual is int)))
                throw new ArgumentTypeException(task, expected, actual, arglist, output);
        }

        /// <summary>
        /// Check the specified argument value is of the right type.
        /// If so, return the argument cast to the type.  If not, throw exception
        /// </summary>
        /// <param name="task">Name of task - used in error message if necessary</param>
        /// <param name="actual">Value provided</param>
        /// <param name="arglist">Full argument list of the task</param>
        /// <exception cref="ArgumentTypeException">When value isn't of the expected type</exception>
        public static TExpected Cast<TExpected>(object task, object? actual, object?[]? arglist, TextBuffer output)
        {
            if (typeof(TExpected) == typeof(object))
                return (TExpected)actual!;
            Check(task, typeof(TExpected), actual, arglist, output);
            if (typeof(TExpected) == typeof(float))
                return (TExpected)(object)Convert.ToSingle(actual);
            return (TExpected) actual!;
        }

        public static object?[] CastArrayTuple(object task, object? actual, object?[]? arglist, BindingList? bindings, TextBuffer output)
        {
            if (actual is Pair p)
            {
                var a = Pair.CompressPairChainsWhenPossible(p, bindings) as object?[];
                if (a != null)
                    return a;
                throw new ArgumentTypeException(task, typeof(object?[]), actual, arglist, output);
            }

            return Cast<object?[]>(task, actual, arglist, output);
        }
    }
}