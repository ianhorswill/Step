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

namespace Step.Interpreter
{
    /// <summary>
    /// Implementations of built-in, but first-order primitives
    /// Higher-order primitives are in HigherOrderBuiltins.cs
    /// </summary>
    internal static class Builtins
    {
        private static readonly object[] EmptyArray = new object[0];
        internal static PrimitiveTask WritePrimitive;

        /// <summary>
        /// Add the built-in primitives to the global module.
        /// </summary>
        internal static void DefineGlobals()
        {
            var g = Module.Global;

            g["="] = new GeneralPrimitive("=", (args, o, e, predecessor, k) =>
            {
                ArgumentCountException.Check("=", 2, args);
                return e.Unify(args[0], args[1], e.Unifications, out var newBindings) &&
                       k(o, newBindings, e.State, predecessor);
            });
            g[">"] = new SimplePredicate<float, float>(">", (a, b) => a > b);
            g["<"] = new SimplePredicate<float, float>("<", (a, b) => a < b);
            g[">="] = new SimplePredicate<float, float>(">=", (a, b) => a >= b);
            g["<="] = new SimplePredicate<float, float>("<=", (a, b) => a <= b);
            g["Paragraph"] = new DeterministicTextGenerator("Paragraph",
                () => new[] {TextUtilities.NewParagraphToken});
            g["NewLine"] = new DeterministicTextGenerator("NewLine",
                () => new[] {TextUtilities.NewLineToken});
            g["FreshLine"] = new DeterministicTextGenerator("FreshLine",
                () => new[] {TextUtilities.FreshLineToken});
            g["ForceSpace"] = new DeterministicTextGenerator("ForceSpace",
                () => new[] { TextUtilities.ForceSpaceToken });
            g["Fail"] = new SimplePredicate("Fail", () => false);
            g["Break"] = new SimplePredicate("Break", Break);
            g["Throw"] = new SimpleNAryPredicate("Throw", Throw);
            g["StringForm"] = 
                new GeneralPredicate<object, string>("StringForm",
                    (o, s) => o.ToString() == s,
                    o => new [] { o.ToString() },
                    null, null);

            WritePrimitive = new DeterministicTextMatcher("Write", (o =>
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

            g["WriteWithoutUnderscores"] = new DeterministicTextMatcher("WriteWithoutUnderscores", (o =>
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

            g["WriteWithoutUnderscoresCapitalized"] = new DeterministicTextMatcher("WriteWithoutUnderscoresCapitalized", (o =>
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

            g["WriteCapitalized"] = new DeterministicTextMatcher("WriteCapitalized", (o =>
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
            
            g["WriteConcatenated"] = new DeterministicTextGenerator<object, object>("WriteConcatenated",
                (s1, s2) => { return new[] {$"{s1}{s2}"}; });

            g["Member"] = new GeneralPredicate<object, IEnumerable<object>>("Member", (member, collection) => collection != null && collection.Contains(member), null, collection => collection ?? EmptyArray, null);
            g["Var"] = new SimplePredicate<object>("Var", o => o is LogicVariable);
            g["NonVar"] = new SimplePredicate<object>("NonVar", o => !(o is LogicVariable));
            g["String"] = new SimplePredicate<object>("String", o => o is string);
            g["Number"] = new SimplePredicate<object>("Number", o => o is int || o is float);
            g["Tuple"] = new SimplePredicate<object>("Tuple", o => o is object[]);
            g["BinaryTask"] = new SimplePredicate<object>("BinaryTask",
                o => (o is Task c && c.ArgumentCount == 2));
            g["Empty"] = Cons.Empty;

            g["CountAttempts"] = new GeneralPrimitive("CountAttempts", (args, o, bindings, p, k) =>
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
            });
            
            g["RandomIntegerInclusive"] = new SimpleFunction<int, int, int>("RandomIntegerInclusive", Randomizer.IntegerInclusive);

            g["RandomIntegerExclusive"] = new SimpleFunction<int, int, int>("RandomIntegerExclusive", Randomizer.IntegerExclusive);

            g["StartsWithVowel"] = new SimplePredicate<object>("StartsWithVowel",
                x =>
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
                });

            g["NounSingularPlural"] = 
                new GeneralPredicate<string, string>("NounSingularPlural", (s, p) => Inflection.PluralOfNoun(s) == p, s => new[] {Inflection.PluralOfNoun(s)}, p => new[] {Inflection.SingularOfNoun(p)}, null);
            g["EnvironmentOption"] = new SimpleNAryPredicate("EnvironmentOption",
                arglist =>
                {
                    ArgumentCountException.CheckAtLeast("EnvironmentOption", 1, arglist);
                    var optionName = ArgumentTypeException.Cast<string>("EnvironmentOption", arglist[0], arglist);
                    EnvironmentOption.Handle(optionName, arglist.Skip(1).ToArray());
                    return true;
                });

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

        private static bool Throw(object[] args)
        {
            string Stringify(object o)
            {
                var s = o.ToString();
                if (s != "")
                    return s;
                return o.GetType().Name;
            }

            throw new Exception(args.Select(Stringify).Untokenize());
        }
    }
}
