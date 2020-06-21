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
    /// Task implemented in C# code
    /// </summary>
    public static class PrimitiveTask
    {
        public delegate bool Predicate1(object arg1);
        public delegate bool Predicate2(object arg1, object arg2);

        public static Predicate1 Predicate<T>(string name, Func<T, bool> realFunction)
        {
            return o =>
            {
                ArgumentTypeException.Check(name, typeof(T), o);
                return realFunction((T) o);
            };
        }

        public static Predicate2 Predicate<T1,T2>(string name, Func<T1, T2, bool> realFunction)
        {
            return (o1, o2) =>
            {
                ArgumentTypeException.Check(name, typeof(T1), o1);
                ArgumentTypeException.Check(name, typeof(T2), o2);
                return realFunction((T1)o1, (T2)o2);
            };
        }

        public delegate IEnumerable<string> DeterministicTextGenerator0();
        public delegate IEnumerable<string> DeterministicTextGenerator1(object arg1);
        public delegate IEnumerable<string> DeterministicTextGenerator2(object arg1, object arg2);

        public delegate IEnumerable<IEnumerable<string>> NondeterministicTextGenerator0();
        public delegate IEnumerable<IEnumerable<string>> NondeterministicTextGenerator1(object arg1);
        public delegate IEnumerable<IEnumerable<string>> NondeterministicTextGenerator2(object arg1, object arg2);

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
