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
using System.Collections;
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
            }).Arguments("a", "b")
                .Documentation("comparison", "Matches (unifies) a and b, and succeeds when they're the same.");

            g["Different"] = new SimplePredicate<object,object>("Different",
                (a, b) => !a.Equals(b) && !(a is LogicVariable) && !(b is LogicVariable))
                .Arguments("a","b")
                .Documentation("comparison", "Attempts to match a and b and succeeds if they *can't* be matched");

            g[">"] = new SimplePredicate<float, float>(">", (a, b) => a > b)
                .Arguments("a", "b")
                .Documentation("comparison", "True when a and b are both numbers and a is larger");
            g["<"] = new SimplePredicate<float, float>("<", (a, b) => a < b)
                .Arguments("a", "b")
                .Documentation("comparison", "True when a and b are both numbers and a is smaller");
            g[">="] = new SimplePredicate<float, float>(">=", (a, b) => a >= b)
                .Arguments("a", "b")
                .Documentation("comparison", "True when a and b are both numbers and a is at least as large as b");
            g["<="] = new SimplePredicate<float, float>("<=", (a, b) => a <= b)
                .Arguments("a", "b")
                .Documentation("comparison", "True when a and b are both numbers and a is no larger than b");

            g["Paragraph"] = new DeterministicTextGenerator("Paragraph",
                () => new[] {TextUtilities.NewParagraphToken})
                .Arguments()
                .Documentation("output", "Starts a new paragraph");
            g["NewLine"] = new DeterministicTextGenerator("NewLine",
                () => new[] {TextUtilities.NewLineToken})
                .Arguments()
                .Documentation("output", "Starts a new line");
            g["FreshLine"] = new DeterministicTextGenerator("FreshLine",
                () => new[] {TextUtilities.FreshLineToken})
                .Arguments()
                .Documentation("output", "Starts a new line, unless we're already at the start of a new line");
            g["ForceSpace"] = new DeterministicTextGenerator("ForceSpace",
                () => new[] { TextUtilities.ForceSpaceToken })
                .Arguments()
                .Documentation("output", "Forces a space to be inserted between two tokens that wouldn't normally be separated.  For example, \"a .\" prints as \"a.\" but \"a [ForceSpace] .\" prints as \"a .\"");

            g["Fail"] = new SimplePredicate("Fail", () => false)
                .Arguments()
                .Documentation("control", "Never succeeds; forces the system to backtrack immediately.");

            g["Break"] = new SimplePredicate("Break", Break)
                .Arguments()
                .Documentation("control", "Breakpoint; pauses execution and displays the current stack in the debugger.");

            g["Throw"] = new SimpleNAryPredicate("Throw", Throw)
                .Arguments("message", "...")
                .Documentation("control", "Throws an exception (error) containing the specified message.");

            g["StringForm"] = 
                new GeneralPredicate<object, string>("StringForm",
                    (o, s) => o.ToString() == s,
                    o => new [] { o.ToString() },
                    null, null)
                    .Arguments("object", "?string_form")
                    .Documentation("output", "Matches ?string_form with the printed representation of object");

            g["WriteVerbatim"] = new DeterministicTextMatcher("WriteVerbatim", (o =>
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
            }))
                .Arguments("object")
                .Documentation("output", "Prints object; _'s are printed as themselves rather than changed to spaces,");
            
            WritePrimitive = new DeterministicTextMatcher("Write", (o =>
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

            g["Write"] = WritePrimitive
                .Arguments("object")
                .Documentation("output", "Prints object, transforming _'s to spaces");

            g["WriteCapitalized"] = new DeterministicTextMatcher("WriteCapitalized", (o =>
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
            }))
                .Arguments("object")
                .Documentation("output", "Prints object, transforming _'s to spaces.  If the first character of the output is a lower-case letter, it will capitalize it.");

            g["WriteConcatenated"] = new DeterministicTextGenerator<object, object>("WriteConcatenated",
                (s1, s2) => { return new[] {$"{Writer.TermToString(s1).Replace('_', ' ')}{Writer.TermToString(s2).Replace('_', ' ')}"}; })
                .Arguments("object1", "object2")
                .Documentation("output", "Prints both objects, without a space between them, and changes and _'s to spaces.");

            g["Member"] = new GeneralPredicate<object, IEnumerable<object>>("Member", (member, collection) => collection != null && collection.Contains(member), null, collection => collection ?? EmptyArray, null)
                .Arguments("element", "collection")
                .Documentation("data structures", "True when element is an element of collection.");
            g["Var"] = new SimplePredicate<object>("Var", o => o is LogicVariable)
                .Arguments("x")
                .Documentation("metalogical", "Succeeds when its argument is an uninstantiated variable (a variable without a value)");
            g["NonVar"] = new SimplePredicate<object>("NonVar", o => !(o is LogicVariable))
                .Arguments("x")
                .Documentation("metalogical", "Succeeds when its argument is a *not* an uninstantiated variable.");
            g["String"] = new SimplePredicate<object>("String", o => o is string)
                .Arguments("x")
                .Documentation("type testing", "Succeeds when its argument is a string");
            g["Number"] = new SimplePredicate<object>("Number", o => o is int || o is float || o is double)
                .Arguments("x")
                .Documentation("type testing", "Succeeds when its argument is a number");
            g["Tuple"] = new SimplePredicate<object>("Tuple", o => o is object[])
                .Arguments("x")
                .Documentation("type testing", "Succeeds when its argument is a tuple");
            g["BinaryTask"] = new SimplePredicate<object>("BinaryTask",
                o => (o is Task c && c.ArgumentCount == 2))
                .Arguments("x")
                .Documentation("type testing", "Succeeds when its argument is 2-argument task");
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
            }).Arguments("?count")
                .Documentation("control", "Binds ?count to 0, then to increasing numbers each time the system backtracks to the call.  Used in a loop to run something repeatedly: [CountAttempts ?count] [DoSomething] [= ?count 100] will run DoSomething until ?count is 100.");
            
            g["RandomIntegerInclusive"] = new SimpleFunction<int, int, int>("RandomIntegerInclusive", Randomization.IntegerInclusive)
                .Arguments("min", "max", "?random")
                .Documentation("randomization", "Sets ?random to a random integer such that min <= ?random <= max");

            g["RandomIntegerExclusive"] = new SimpleFunction<int, int, int>("RandomIntegerExclusive", Randomization.IntegerExclusive)
                .Arguments("min", "max", "?random")
                .Documentation("randomization","Sets ?random to a random integer such that min <= ?random < max");

            g["RandomElement"] = new GeneralPredicate<IList, object>(
                "RandomElement",
                (list, elt) => list.Contains(elt),
                list => list.BadShuffle().Cast<object>(),
                null,
                null)
                .Arguments("list", "?element")
                .Documentation("randomization","Sets ?element to a random element of list.  If this is backtracked, it generates a random shuffle of the elements of this list.  However, not all shuffles are possible; it starts with a random element and moves to subsequent elements with a random step size.");

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
                })
                .Arguments("string")
                .Documentation("string processing","True if the string starts with a vowel.");

            g["NounSingularPlural"] = 
                new GeneralPredicate<string, string>("NounSingularPlural", (s, p) => Inflection.PluralOfNoun(s) == p, s => new[] {Inflection.PluralOfNoun(s)}, p => new[] {Inflection.SingularOfNoun(p)}, null)
                    .Arguments("?singular", "?plural")
                    .Documentation("string processing", "True if ?plural is the English plural form of ?singular");

            g["EnvironmentOption"] = new SimpleNAryPredicate("EnvironmentOption",
                arglist =>
                {
                    ArgumentCountException.CheckAtLeast("EnvironmentOption", 1, arglist);
                    var optionName = ArgumentTypeException.Cast<string>("EnvironmentOption", arglist[0], arglist);
                    EnvironmentOption.Handle(optionName, arglist.Skip(1).ToArray());
                    return true;
                })
                .Arguments("argument", "...")
                .Documentation("Asks StepRepl or whatever other program this Step code is running in to change its handling of step code.");

            g["Hashtable"] = new SimpleNAryFunction(
                "Hashtable",
                data =>
                {
                    if ((data.Length % 2) != 0)
                        throw new ArgumentException(
                            "Hashtable requires an odd number of arguments, one for the output and an equal number of keys and values");
                    var h = new Hashtable();
                    for (var i = 0; i < data.Length; i += 2)
                        h[data[i]] = data[i + 1];
                    return h;
                })
                .Arguments("?h")
                .Documentation("Creates a new empty hash table and stores it in ?h");

            g["Contains"] =
                new SimplePredicate<string, string>("Contains", (super, sub) => super.Contains(sub))
                    .Arguments("string", "substring")
                    .Documentation("string processing", "True if substring is a substring of string");

            HigherOrderBuiltins.DefineGlobals();
            ReflectionBuiltins.DefineGlobals();
            Documentation.DefineGlobals(Module.Global);
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
