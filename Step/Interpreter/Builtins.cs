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
using Step.Utilities;
using static Step.Interpreter.PrimitiveTask;

namespace Step.Interpreter
{
    /// <summary>
    /// Implementations of built-in, but first-order primitives
    /// Higher-order primitives are in HigherOrderBuiltins.cs
    /// </summary>
    internal static class Builtins
    {
        private static readonly object[] EmptyArray = new object[0];
        internal static MetaTask WritePrimitive;

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
            g["Paragraph"] = NamePrimitive("Paragraph",
                (DeterministicTextGenerator0) (() => new[] {TextUtilities.NewParagraphToken}));
            g["NewLine"] = NamePrimitive("NewLine",
                (DeterministicTextGenerator0) (() => new[] {TextUtilities.NewLineToken}));
            g["FreshLine"] = NamePrimitive("FreshLine",
                (DeterministicTextGenerator0) (() => new[] {TextUtilities.FreshLineToken}));
            g["ForceSpace"] = NamePrimitive("ForceSpace",
                (DeterministicTextGenerator0)(() => new[] { TextUtilities.ForceSpaceToken }));
            g["Fail"] = NamePrimitive("Fail", (Predicate0) (() => false));
            g["Break"] = NamePrimitive("Break", (Predicate0) Break);
            g["Throw"] = NamePrimitive("Throw", (PredicateN) Throw);
            g["StringForm"] = UnaryFunction<object, string>("StringForm", o => o.ToString());

            WritePrimitive = DeterministicTextMatcher("Write", (o =>
            {
                switch (o)
                {
                    case null:
                        return new[] {"null"};
                    case string[] tokens:
                        return tokens;
                    default:
                        return new[] {Writer.TermToString(o)};
                }
            }));
            g["Write"] = WritePrimitive;

            g["WriteWithoutUnderscores"] = DeterministicTextMatcher("WriteWithoutUnderscores", (o =>
            {
                switch (o)
                {
                    case null:
                        return new[] {"null"};
                    case string[] tokens:
                        return tokens.Length == 0? tokens : tokens.Skip(1).Prepend(tokens[0].Capitalize()).ToArray();
                    default:
                        return new[] {Writer.TermToString(o).Replace('_', ' ')};
                }
            }));

            g["WriteWithoutUnderscoresCapitalized"] = DeterministicTextMatcher("WriteWithoutUnderscoresCapitalized", (o =>
            {
                switch (o)
                {
                    case null:
                        return new[] { "null" };
                    case string[] tokens:
                        return tokens.Length == 0 ? tokens : tokens.Skip(1).Prepend(tokens[0].Capitalize()).ToArray();
                    default:
                        return new[] { Writer.TermToString(o).Replace('_', ' ').Capitalize() };
                }
            }));

            g["WriteCapitalized"] = DeterministicTextMatcher("WriteCapitalized", (o =>
            {
                switch (o)
                {
                    case null:
                        return new[] { "null" };
                    case string[] tokens:
                        return tokens;
                    default:
                        return new[] { Writer.TermToString(o).Replace('_', ' ').Capitalize() };
                }
            }));
            
            g["WriteConcatenated"] = NamePrimitive("Write",
                (DeterministicTextGenerator2) ((s1, s2) => { return new[] {$"{s1}{s2}"}; }));

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

            g["CountAttempts"] = NamePrimitive("CountAttempts", (MetaTask) ((args, o, bindings, k, p) =>
            {
                ArgumentCountException.Check("CountAttempts", 1, args);
                ArgumentInstantiationException.Check("CountAttempts", args[0], false, bindings, args);
                int count = 0;
                while (true)
                    if (k(o,
                        BindingList<LogicVariable>.Bind(bindings.Unifications, (LogicVariable) args[0], count++),
                        bindings.State,
                        p))
                        return true;
                // ReSharper disable once FunctionNeverReturns
            }));
            
            g["RandomIntegerInclusive"] = NamePrimitive("RandomIntegerInclusive",
                SimpleFunction<int, int, int>("RandomIntegerInclusive", Randomizer.IntegerInclusive));

            g["RandomIntegerExclusive"] = NamePrimitive("RandomIntegerExclusive",
                SimpleFunction<int, int, int>("RandomIntegerExclusive", Randomizer.IntegerExclusive));

            g["StartsWithVowel"] = NamePrimitive("StartsWithVowel",
                (Predicate1)(x =>
                {
                    switch (x)
                    {
                        case string s:
                            return StartsWithVowel(s);
                        case string[] tokens:
                            return tokens.Length > 0 && StartsWithVowel(tokens[0]);
                        default:
                            return false;
                    }
                }));

            g["NounSingularPlural"] = NamePrimitive("NounSingularPlural",
                GeneralRelation<string, string>("NounSingularPlural",
                    (s, p) => Inflection.PluralOfNoun(s) == p,
                    s => new[] {Inflection.PluralOfNoun(s)},
                    p => new[] {Inflection.SingularOfNoun(p)},
                    null));

            HigherOrderBuiltins.DefineGlobals();
            ReflectionBuiltins.DefineGlobals();
        }

        private static bool StartsWithVowel(string x)
        {
            // ReSharper disable once StringLiteralTypo
            return x.Length > 0 && "aeiou".Contains(x[0]);
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
