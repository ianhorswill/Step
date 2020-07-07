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

using System;
using System.Text;

namespace Step.Interpreter
{
    /// <summary>
    /// Signals a task was called with an arg bound/unbound when it shouldn't have been
    /// </summary>
    public class ArgumentInstantiationException : ArgumentException
    {
        public ArgumentInstantiationException(object task, BindingEnvironment e, object[] args) 
            : base($"Arguments to {task} incorrectly instantiated: {PrintArgs(args, e)}")
        { }

        private static string PrintArgs(object[] args, BindingEnvironment e)
        {
            var b = new StringBuilder();
            foreach (var a in args)
            {
                b.Append(e.Resolve(a));
                b.Append(' ');
            }

            return b.ToString();
        }

        public static void Check(object task, int expected, object[] actual)
        {
            if (expected != actual.Length)
                throw new ArgumentCountException(task, expected, actual);
        }
    }
}
