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
using System.Collections.Generic;
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
        private static readonly object[] EmptyArray = new object[0];

        /// <summary>
        /// Add the built-in primitives to the global module.
        /// </summary>
        internal static void DefineGlobals()
        {
            var g = Module.Global;

            g["="] = NamePrimitive("=", (MetaTask) ((args, o, e, k, predecessor) =>
            {
                ArgumentCountException.Check("=", 2, args);
                return e.Unify(args[0], args[1], e.Unifications, out var newBindings) &&
                           k(o, newBindings, e.State, predecessor);
            }));
            g[">"] = Predicate<float, float>(">", (a, b) => a > b);
            g["<"] = Predicate<float, float>("<", (a, b) => a < b);
            g[">="] = Predicate<float, float>(">=", (a, b) => a >= b);
            g["<="] = Predicate<float, float>("<=", (a, b) => a <= b);
            g["Paragraph"] = NamePrimitive("Paragraph",(DeterministicTextGenerator0) (() => NewLine));
            g["Fail"] = NamePrimitive("Fail", (Predicate0)(() => false));
            g["Break"] = NamePrimitive("Break", (Predicate0) Break);
            g["Throw"] = NamePrimitive("Throw",(PredicateN) Throw);
            g["StringForm"] = UnaryFunction<object,string>("StringForm", o => o.ToString());
            g["Write"] = NamePrimitive("Write", (DeterministicTextGenerator1) (o =>
            {
                if (o == null)
                    return new[] {"null"};
                if (o is string[] tokens)
                    return tokens;
                return new[] {o.ToString()};
            }));
            g["Member"] = GeneralRelation<object, IEnumerable<object>>(
                "Member",
                (member, collection) => collection != null && collection.Contains(member),
                null,
                collection => collection ?? EmptyArray,
                null);
            g["Number"] = Predicate<object>("Number", o => o != null && (o is int || o is float));
            g["Var"] = Predicate<object>("Var", o => o is LogicVariable);
            g["NonVar"] = Predicate<object>("NonVar", o => !(o is LogicVariable));
            g["String"] = Predicate<object>("String", o => o is string);
            g["Tuple"] = Predicate<object>("Tuple", o => o is object[]);
            g["BinaryTask"] = Predicate<object>("BinaryTask", 
                o =>
                {
                    o = GetSurrogate(o);
                    return ((o is CompoundTask c && c.ArgCount == 2) || o is Predicate2 ||
                            o is DeterministicTextGenerator2 || o is NondeterministicTextGenerator2 ||
                            o is NonDeterministicRelation);
                });
            g["Empty"] = Cons.Empty;

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
