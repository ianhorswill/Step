#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Builtins.cs" company="Ian Horswill">
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
using System.Diagnostics;
using System.Linq;
using static Step.Interpreter.PrimitiveTask;

namespace Step.Interpreter
{
    /// <summary>
    /// Implementations of built-in, but first-order primitives
    /// Higher-order primitives are in HigherOrderBuiltins.cs
    /// </summary>
    internal static class Builtins
    {
        private static readonly string[] NewLine = { "\n" };
        /// <summary>
        /// Add the built-in primitives to the global module.
        /// </summary>
        internal static void DefineGlobals()
        {
            var g = Module.Global;

            g["="] = (MetaTask) ((args, o, e, k) =>
            {
                ArgumentCountException.Check("=", 2, args);
                return e.Unify(args[0], args[1], e.Unifications, out var newBindings) &&
                           k(o, newBindings, e.DynamicState);
            });
            g[">"] = Predicate<float, float>(">", (a, b) => a > b);
            g["<"] = Predicate<float, float>("<", (a, b) => a < b);
            g[">="] = Predicate<float, float>(">=", (a, b) => a >= b);
            g["<="] = Predicate<float, float>("<=", (a, b) => a <= b);
            g["Newline"] = (DeterministicTextGenerator0) (() => NewLine);
            g["Fail"] = (Predicate0)(() => false);
            g["Break"] = (Predicate0) Break;
            g["Throw"] = (PredicateN) Throw;
            g["StringForm"] = UnaryFunction<object,string>("StringForm", o => o.ToString());
            g["Write"] = (DeterministicTextGenerator1) (o => new []{ o.ToString() });

            HigherOrderBuiltins.DefineGlobals();
        }

        private static bool Break()
        {
            Debugger.Break();
            return true;
        }

        private static bool Throw(object[] args, BindingEnvironment e)
        {
            string Stringify(object o)
            {
                var s = o.ToString();
                if (s != "")
                    return s;
                return o.GetType().Name;
            }

            throw new Exception(e.ResolveList(args).Select(Stringify).Untokenize());
        }
    }
}
