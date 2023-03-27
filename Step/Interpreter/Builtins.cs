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
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Step.Output;
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
        internal static PrimitiveTask? WritePrimitive;

        /// <summary>
        /// Add the built-in primitives to the global module.
        /// </summary>
        internal static void DefineGlobals()
        {
            var g = Module.Global;

            Documentation.SectionIntroduction("comparison",
                "Predicates that test whether two values are the same or different.  Many of these use unification, in which case they are testing whether the values can be made identical through binding variables.");

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

            Documentation.SectionIntroduction("output",
                "Tasks that print things.");

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

            Documentation.SectionIntroduction("control flow//controlling backtracking",
                "Tasks that control how or whether execution backtracks.");

            g["Fail"] = new SimplePredicate("Fail", () => false)
                .Arguments()
                .Documentation("control flow//controlling backtracking", "Never succeeds; forces the system to backtrack immediately.");

            Documentation.SectionIntroduction("debugging",
                "Tasks used to help debug code.");

            g["Break"] = new SimplePredicate("Break", Break)
                .Arguments()
                .Documentation("debugging", "Breakpoint; pauses execution and displays the current stack in the debugger.");

            g["Throw"] = new SimpleNAryPredicate("Throw", Throw)
                .Arguments("message", "...")
                .Documentation("control flow", "Throws an exception (error) containing the specified message.");

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

            Documentation.SectionIntroduction("data structures",
                "Predicates that create or access complex data objects.  Note that dictionaries and lists can also be used as predicates.  So [dictionary ?key ?value] is true when ?key has ?value in the dictionary and and [list ?element] is true when ?element is an element of the list.");

            Documentation.SectionIntroduction("data structures//lists",
                "Predicates access lists in particular.  These work with any C# object that implements the IList interface, including Step tuples (which are the C# type object[]).");

            g["Member"] = new GeneralPredicate<object, IEnumerable<object>>("Member", (member, collection) => collection != null && collection.Contains(member), null, collection => collection ?? EmptyArray, null)
                .Arguments("element", "collection")
                .Documentation("data structures//lists", "True when element is an element of collection.");

            g["Length"] = new SimpleFunction<IList, int>("Length", l => l.Count)
                .Arguments("list", "?length")
                .Documentation("data structures//list", "True when list has exactly ?length elements");

            g["Nth"] = new GeneralNAryPredicate("Nth",
                args =>
                {
                    ArgumentCountException.Check("Nth", 3, args);
                    var list = ArgumentTypeException.Cast<IList>("Nth", args[0], args);
                    var indexVar = args[1] as LogicVariable;
                    var elementVar = args[2] as LogicVariable;

                    if (indexVar == null)
                    {
                        var index = ArgumentTypeException.Cast<int>("Nth", args[1], args);
                        return new[] {new[] {list, index, list[index]}};
                    }
                    else if (elementVar == null)
                    {
                        var elt = args[2];
                        var index = list.IndexOf(elt);
                        if (index >= 0)
                            return new[] {new[] {list, index, args[2]}};
                        else
                            return new object[0][];
                    }

                    throw new ArgumentInstantiationException("Nth", new BindingEnvironment(), args);
                })
                .Arguments("list", "index", "?element")
                .Documentation("data structures//list", "True when element of list at index is ?element");

            g["Cons"] = new GeneralNAryPredicate("Cons", args =>
            {
                ArgumentCountException.Check("Cons", 3, args);
                if (args[2] is object[] tuple)
                    return new[] {new[] { tuple[0], tuple.Skip(1).ToArray(), tuple }};
                if (args[1] is object[] restTuple)
                    return new[] {new[] {args[0], restTuple, restTuple.Prepend(args[0]).ToArray() } };
                throw new ArgumentException("Either the second or argument of Cons must be a tuple.");
            })
                .Arguments("firstElement", "restElements", "tuple")
                .Documentation("True when tuple starts with firstElement and continues with restElements.");

            Documentation.SectionIntroduction("metalogical",
                "Predicates that test the binding state of a variable.");

            g["Var"] = new SimplePredicate<object>("Var", o => o is LogicVariable)
                .Arguments("x")
                .Documentation("metalogical", "Succeeds when its argument is an uninstantiated variable (a variable without a value)");
            g["NonVar"] = new SimplePredicate<object>("NonVar", o => !(o is LogicVariable))
                .Arguments("x")
                .Documentation("metalogical", "Succeeds when its argument is a *not* an uninstantiated variable.");
            g["Ground"] = new SimplePredicate<object>("Ground", Term.IsGround)
                .Arguments("x")
                .Documentation("metalogical", "Succeeds when its argument is contains no uninstantiated variables (variables without values)");
            g["Nonground"] = new SimplePredicate<object>("Nonground", o => !Term.IsGround(o))
                .Arguments("x")
                .Documentation("metalogical", "Succeeds when its argument is contains uninstantiated variables (variables without values)");


            g["CopyTerm"] = new GeneralPrimitive("CopyTerm",
                (args, t, b, f, k) =>
                {
                    ArgumentCountException.Check("CopyTerm", 2, args);
                    return b.Unify(args[1], b.CopyTerm(args[0]), out var u) && k(t, u, b.State, f);
                })
                .Arguments("in", "out")
                .Documentation("metalogical",
                    "Sets out to a copy of in with fresh variables, so that unifications to one don't affect the other");

            Documentation.SectionIntroduction("type testing",
                "Predicates that test what type of data object their argument is.  These fail when the argument is an unbound variable.");

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
                o => o is Task { ArgumentCount: 2 })
                .Arguments("x")
                .Documentation("type testing", "Succeeds when its argument is 2-argument task");
            g["Empty"] = Cons.Empty;
            g["EmptyMaxQueue"] = ImmutableSortedSet.Create<(object element, float priority)>(PriorityQueueComparer.Max);
            g["EmptyMinQueue"] = ImmutableSortedSet.Create<(object element, float priority)>(PriorityQueueComparer.Min);

            g["CountAttempts"] = new GeneralPrimitive("CountAttempts", (args, o, bindings, p, k) =>
            {
                ArgumentCountException.Check("CountAttempts", 1, args);
                ArgumentInstantiationException.Check("CountAttempts", args[0], false, bindings, args);
                int count = 0;
                while (true)
                    if (k(o,
                        BindingList.Bind(bindings.Unifications, (LogicVariable) args[0]!, count++),
                        bindings.State,
                        p))
                        return true;
                // ReSharper disable once FunctionNeverReturns
            }).Arguments("?count")
                .Documentation("control flow", "Binds ?count to 0, then to increasing numbers each time the system backtracks to the call.  Used in a loop to run something repeatedly: [CountAttempts ?count] [DoSomething] [= ?count 100] will run DoSomething until ?count is 100.");

            Documentation.SectionIntroduction("randomization",
                "Tasks that choose random numbers or list elements.");

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

            Documentation.SectionIntroduction("string processing",
                "Predicates that test the spelling of strings.");

            g["Format"] = new SimpleFunction<string, object[], string>("Format", string.Format)
                .Arguments("format_string, argument_list, ?formatted_string")
                .Documentation("string processing",
                    "True when formatted_string is the result of formatting format_string with the arguments.  This is just a wrapper for .NET's string.Format routine.");

            g["Downcase"] = new SimpleFunction<string,string>("DownCase", from =>
                {
                    var b = new StringBuilder();
                    foreach (var c in from)
                        b.Append(char.ToLower(c));
                    return b.ToString();
                })
                .Arguments("string, ?downcased")
                .Documentation("string processing",
                    "True when downcased is the string with all alphabetic characters converted to lowercase.");

            g["Downcased"] = new SimpleFunction<string, string>("Downcased", from =>
                {
                    var b = new StringBuilder();
                    foreach (var c in from)
                        b.Append(char.ToLower(c));
                    return b.ToString();
                })
                .Arguments("string, ?downcased")
                .Documentation("string processing",
                    "True when downcased is the string with all alphabetic characters converted to lowercase.");
            
            g["Upcased"] = new SimpleFunction<string, string>("Upcased", from =>
                {
                    var b = new StringBuilder();
                    foreach (var c in from)
                        b.Append(char.ToUpper(c));
                    return b.ToString();
                })
                .Arguments("string, ?upcased")
                .Documentation("string processing",
                    "True when upcased is the string with all alphabetic characters converted to uppercase.");

            g["Capitalized"] = new SimpleFunction<string, string>("Downcase", from =>
                {
                    var b = new StringBuilder();
                    var startOfWord = true;
                    foreach (var c in from)
                    {
                        b.Append(startOfWord ? char.ToUpper(c) : c);
                        startOfWord = c == ' ' || c == '_';

                    }
                    return b.ToString();
                })
                .Arguments("string, ?capitalized")
                .Documentation("string processing",
                    "True when capitalized is the a copy of string, which the start of each word capitalized.");


           g["StartsWithVowel"] = new SimplePredicate<object>("StartsWithVowel",
                x =>
                {
                    switch (x)
                    {
                        case string s:
                            return TextUtilities.StartsWithVowel(s);
                        case string[] tokens:
                            return tokens.Length > 0 && TextUtilities.StartsWithVowel(tokens[0]);
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

            Documentation.SectionIntroduction("StepRepl",
                "Tasks that control the behavior of StepRepl or whatever other game engine the Step code is running inside of.");

            g["EnvironmentOption"] = new SimpleNAryPredicate("EnvironmentOption",
                arglist =>
                {
                    ArgumentCountException.CheckAtLeast("EnvironmentOption", 1, arglist);
                    var optionName = ArgumentTypeException.Cast<string>("EnvironmentOption", arglist[0], arglist);
                    EnvironmentOption.Handle(optionName, arglist.Skip(1).ToArray());
                    return true;
                })
                .Arguments("argument", "...")
                .Documentation("StepRepl", "Asks StepRepl or whatever other program this Step code is running in to change its handling of step code.");

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
                .Documentation("data structures", "Creates a new empty hash table and stores it in ?h");

            g["Contains"] =
                new SimplePredicate<string, string>("Contains", (super, sub) => super.Contains(sub))
                    .Arguments("string", "substring")
                    .Documentation("string processing", "True if substring is a substring of string");

            HigherOrderBuiltins.DefineGlobals();
            ReflectionBuiltins.DefineGlobals();
            Documentation.DefineGlobals(Module.Global);
        }

        // ReSharper disable once UnusedMember.Global
        public static void DefineFileSystemBuiltins(Module m)
        {
            void ImportFunction(string name, Func<string, string> implementation)
            {
                m[name] = new SimpleFunction<string, string>(name, implementation);
            }
            
            ImportFunction("PathExtension", Path.GetExtension);
            m["PathStructure"] = new GeneralNAryPredicate("PathStructure",
                args =>
                {
                    ArgumentCountException.Check("PathStructure", 3, args);
                    if (args[2] is LogicVariable)
                    {
                        // Path argument uninstantiated
                        args[2] = Path.Combine(ArgumentTypeException.Cast<string>("PathStructure", args[0], args),
                            ArgumentTypeException.Cast<string>("PathStructure", args[1], args));
                        return new[] {args};
                    }
                    else
                    {
                        var path = ArgumentTypeException.Cast<string>("PathStructure", args[2], args);
                        // Path argument is instantiated
                        return new[] {new object[] {Path.GetDirectoryName(path), Path.GetFileName(path), path}};
                    }
                });

            m["DirectoryFile"] = new GeneralPredicate<string, string>("DirectoryFile",
                (d, f) => (File.Exists(f) && Path.GetDirectoryName(f) == d),
                d =>
                {
                    if (Directory.Exists(d))
                        return Directory.GetFiles(d);
                    return new string[0];
                },
                f =>
                {
                    if (Directory.Exists(f))
                        return new[] {Path.GetDirectoryName(f)};
                    return new string[0];
                },
                null);

            m["DirectorySubdirectory"] = new GeneralPredicate<string, string>("DirectorySubdirectory",
                (d, f) => (File.Exists(f) && Path.GetDirectoryName(f) == d),
                d =>
                {
                    if (Directory.Exists(d))
                        return Directory.GetDirectories(d);
                    return new string[0];
                },
                f =>
                {
                    if (Directory.Exists(f))
                        return new[] { Path.GetDirectoryName(f) };
                    return new string[0];
                },
                null);
        }

        private static bool Break()
        {
            Debugger.Break();
            return true;
        }

        private static bool Throw(object?[] args)
        {
            string Stringify(object? o)
            {
                if (o == null)
                    return "null";
                var s = o.ToString();
                if (s != "")
                    return s;
                return o.GetType().Name;
            }

            throw new Exception(args.Select(Stringify).Untokenize());
        }

        private class PriorityQueueComparer : IComparer<(object element, float priority)>
        {
            private int sign = 1;

            public int Compare((object element, float priority) x, (object element, float priority) y)
            {
                return sign * x.Item2.CompareTo(y.Item2);
            }

            public static PriorityQueueComparer Max = new PriorityQueueComparer();
            public static PriorityQueueComparer Min = new PriorityQueueComparer() { sign = -1 };
        }
    }
}
