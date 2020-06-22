#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrimitiveTask.cs" company="Ian Horswill">
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

namespace Step.Interpreter
{
    /// <summary>
    /// Definitions used in making Tasks that are implemented directly as C# code.
    /// </summary>
    public static class PrimitiveTask
    {
        /// <summary>
        /// A primitive that just succeeds or fails, without generating output
        /// </summary>
        /// <param name="arg1">Argument to the predicate</param>
        /// <returns>Whether the predicate should succeed or fail</returns>
        public delegate bool Predicate1(object arg1);
        /// <summary>
        /// A primitive that just succeeds or fails, without generating output
        /// </summary>
        /// <param name="arg1">Argument to the predicate</param>
        /// <param name="arg2">Argument to the predicate</param>
        /// <returns>Whether the predicate should succeed or fail</returns>
        public delegate bool Predicate2(object arg1, object arg2);

        /// <summary>
        /// Wraps a C# predicate in type checking code.
        /// </summary>
        /// <typeparam name="T">Expected type of the predicate's argument</typeparam>
        /// <param name="name">Task name to give to the predicate</param>
        /// <param name="realFunction">Implementation as a C# delegate</param>
        /// <returns></returns>
        public static Predicate1 Predicate<T>(string name, Func<T, bool> realFunction)
        {
            return o =>
            {
                ArgumentTypeException.Check(name, typeof(T), o);
                return realFunction((T) o);
            };
        }
        /// <summary>
        /// Wraps a C# predicate in type checking code.
        /// </summary>
        /// <typeparam name="T1">Expected type of the predicate's first argument</typeparam>
        /// <typeparam name="T2">Expected type of the predicate's second argument</typeparam>
        /// <param name="name">Task name to give to the predicate</param>
        /// <param name="realFunction">Implementation as a C# delegate</param>
        /// <returns></returns>
        public static Predicate2 Predicate<T1,T2>(string name, Func<T1, T2, bool> realFunction)
        {
            return (o1, o2) =>
            {
                ArgumentTypeException.Check(name, typeof(T1), o1);
                ArgumentTypeException.Check(name, typeof(T2), o2);
                return realFunction((T1)o1, (T2)o2);
            };
        }

        /// <summary>
        /// A primitive task that generates text and always succeeds once (i.e. you can't backtrack to get different alternative versions of the text.
        /// </summary>
        /// <returns>Generated text</returns>
        public delegate IEnumerable<string> DeterministicTextGenerator0();
        /// <summary>
        /// A primitive task that generates text and always succeeds once (i.e. you can't backtrack to get different alternative versions of the text.
        /// </summary>
        /// <returns>Generated text</returns>
        public delegate IEnumerable<string> DeterministicTextGenerator1(object arg1);
        /// <summary>
        /// A primitive task that generates text and always succeeds once (i.e. you can't backtrack to get different alternative versions of the text.
        /// </summary>
        /// <returns>Generated text</returns>
        public delegate IEnumerable<string> DeterministicTextGenerator2(object arg1, object arg2);

        /// <summary>
        /// A primitive task that generates text and succeeds a variable number of times (possibly not at all)
        /// </summary>
        /// <returns>Each element is a string enumeration for one possible success of this primitive.</returns>
        public delegate IEnumerable<IEnumerable<string>> NondeterministicTextGenerator0();
        /// <summary>
        /// A primitive task that generates text and succeeds a variable number of times (possibly not at all)
        /// </summary>
        /// <returns>Each element is a string enumeration for one possible success of this primitive.</returns>
        public delegate IEnumerable<IEnumerable<string>> NondeterministicTextGenerator1(object arg1);
        /// <summary>
        /// A primitive task that generates text and succeeds a variable number of times (possibly not at all)
        /// </summary>
        /// <returns>Each element is a string enumeration for one possible success of this primitive.</returns>
        public delegate IEnumerable<IEnumerable<string>> NondeterministicTextGenerator2(object arg1, object arg2);

        /// <summary>
        /// Add the built-in primitives to the global module.
        /// </summary>
        public static void DefineGlobals()
        {
            var g = Module.Global;
            g["="] = Predicate<int, int>("=", (a, b) => a == b);
            g[">"] = Predicate<int, int>(">", (a, b) => a > b);
            g["<"] = Predicate<int, int>("<", (a, b) => a < b);
            g[">="] = Predicate<int, int>(">=", (a, b) => a >= b);
            g["<="] = Predicate<int, int>("<=", (a, b) => a <= b);
        }
    }
}
