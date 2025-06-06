﻿#region Copyright
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
                ArgumentCountException.Check("=", 2, args, o);
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

            g["Break"] = new SimplePredicate("Break", () =>
                {
                    StepThread.BreakPoint();
                    return true;
                })
                .Arguments()
                .Documentation("debugging","When running under the Step debugger, puts the Step interpreter in single-step mode");

            g["InterpreterBreak"] = new SimplePredicate("InterpreterBreak", Break)
                .Arguments()
                .Documentation("debugging", "Trigger a C# breakpoint inside the Step interpreter.  Don't use this unless you're debugging C# code.");

            g["Log"] = new GeneralPrimitive("Log", (args, o, env, frame, k) =>
            {
                LogEvent.Log(args, frame, env);
                return k(o, env.Unifications, env.State, frame);
            })
            .Arguments("anything", "...")
            .Documentation("debugging", "Adds the specified information to the event log.  This operation is not undone upon backtracking.");

            g["LogBack"] = new GeneralPrimitive("LogBack", (args, o, env, frame, k) =>
                {
                    if (k(o, env.Unifications, env.State, frame))
                        return true;
                    LogEvent.Log(args, frame, env);
                    return false;
                })
                .Arguments("anything", "...")
                .Documentation("debugging", "Adds the specified information to the event log when this call is backtracked.  This operation is not undone upon backtracking.");
            
            g["Listing"] =
                new DeterministicTextGenerator<CompoundTask>("Listing", t => t.Methods.Select(m => m.MethodCode+"\n"));

            g["Throw"] = new SimpleNAryPredicate("Throw", Throw)
                .Arguments("message", "...")
                .Documentation("control flow", "Throws an exception (error) containing the specified message.");

            g["BailOut"] = new SimpleNAryPredicate("BailOut", BailOut)
                .Arguments("message", "...")
                .Documentation("control flow", "Immediately stop execution and display the the specified message, but do not print a stack trace.");

            g["StringForm"] = 
                new GeneralPredicate<object, string>("StringForm",
                    (o, s) => o.ToString() == s,
                    o => new [] { o.ToString() },
                    null, null)
                    .Arguments("object", "?string_form")
                    .Documentation("output", "Matches ?string_form with the printed representation of object");

            Documentation.UserDefinedSystemTask("Mention", "objectToPrint")
                .Documentation("output", "User-defined; Define this task to control the printing of variable values in methods.");

            g["WriteVerbatim"] = new DeterministicTextMatcher("WriteVerbatim", (o, b) =>
            {
                switch (o)
                {
                    case null:
                        return new[] {"null"};
                    case string[] tokens:
                        return tokens;
                    default:
                        return new[] {Writer.TermToString(o, b)};
                }
            })
                .Arguments("object")
                .Documentation("output", "Prints object; _'s are printed as themselves rather than changed to spaces,");
            
            WritePrimitive = new DeterministicTextMatcher("Write", (o,b) =>
            {
                switch (o)
                {
                    case null:
                        return new[] {"null"};
                    case string[] tokens:
                        return tokens.Length == 0? tokens : tokens.Skip(1).Prepend(tokens[0].Capitalize()).ToArray();
                    default:
                        return new[] {Writer.TermToString(o,b).Replace('_', ' ')};
                }
            });

            g["Write"] = WritePrimitive
                .Arguments("object")
                .Documentation("output", "Prints object, transforming _'s to spaces");

            g["WriteCapitalized"] = new DeterministicTextMatcher("WriteCapitalized", (o,b) =>
            {
                switch (o)
                {
                    case null:
                        return new[] { "null" };
                    case string[] tokens:
                        return tokens.Length == 0 ? tokens : tokens.Skip(1).Prepend(tokens[0].Capitalize()).ToArray();
                    default:
                        return new[] { Writer.TermToString(o,b).Replace('_', ' ').Capitalize() };
                }
            })
                .Arguments("object")
                .Documentation("output", "Prints object, transforming _'s to spaces.  If the first character of the output is a lower-case letter, it will capitalize it.");

            g["WriteConcatenated"] = new DeterministicTextGenerator<object, object>("WriteConcatenated",
                (s1, s2) => { return new[] {$"{Writer.TermToString(s1).Replace('_', ' ')}{Writer.TermToString(s2).Replace('_', ' ')}"}; })
                .Arguments("object1", "object2")
                .Documentation("output", "Prints both objects, without a space between them, and changes and _'s to spaces.");

            Documentation.SectionIntroduction("data structures",
                "Predicates that create or access complex data objects.  Note that dictionaries and lists can also be used as predicates.  So [dictionary ?key ?value] is true when ?key has ?value in the dictionary and and [list ?element] is true when ?element is an element of the list.");

            Documentation.SectionIntroduction("data structures//lists",
                "Predicates access tuples/lists in particular.  These work with any C# object that implements the IList interface, including Step tuples (which are the C# type object[]).");

            g["Member"] = new GeneralPredicate<object, IEnumerable<object>>("Member",
                    (member, collection) => collection != null && collection.Contains(member),
                    null,
                    collection => collection ?? EmptyArray,
                    null)
                .Arguments("element", "collection")
                .Documentation("data structures//lists", "True when element is an element of collection.");

            g["Length"] = new SimpleFunction<IList, int>("Length", l => l.Count)
                .Arguments("list", "?length")
                .Documentation("data structures//lists", "True when list has exactly ?length elements");

            g["Nth"] = new GeneralNAryPredicate("Nth",
                (args, output) =>
                {
                    ArgumentCountException.Check("Nth", 3, args, output);
                    var list = ArgumentTypeException.Cast<IList>("Nth", args[0], args, output);
                    var indexVar = args[1] as LogicVariable;
                    var elementVar = args[2] as LogicVariable;

                    if (indexVar == null)
                    {
                        var index = ArgumentTypeException.Cast<int>("Nth", args[1], args, output);
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

                    throw new ArgumentInstantiationException("Nth", new BindingEnvironment(), args, output);
                })
                .Arguments("list", "index", "?element")
                .Documentation("data structures//lists", "True when element of list at index is ?element");

            g["Cons"] = new GeneralNAryPredicate("Cons", (args, output) =>
            {
                ArgumentCountException.Check("Cons", 3, args, output);
                if (args[2] is object[] tuple)
                    return new[] {new[] { tuple[0], tuple.Skip(1).ToArray(), tuple }};
                if (args[1] is object[] restTuple)
                    return new[] {new[] {args[0], restTuple, restTuple.Prepend(args[0]).ToArray() } };
                StepThread.ErrorText(output);
                throw new ArgumentException("Either the second or argument of Cons must be a tuple.");
            })
                .Arguments("firstElement", "restElements", "tuple")
                .Documentation("data structures//lists", "True when tuple starts with firstElement and continues with restElements.");

            g[nameof(HasFeature)] = new GeneralPrimitive(nameof(HasFeature), HasFeature)
                .Arguments("featureStructure", "feature")
                .Documentation("data structures", "True when featureStructure contains a feature with the specified name.");

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
                    ArgumentCountException.Check("CopyTerm", 2, args, t);
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
            g["FeatureStructure"] = new SimplePredicate<object>("FeatureStructure", o => o is FeatureStructure)
                .Arguments("x")
                .Documentation("type testing", "Succeeds when its argument is a feature structure, i.e. { feature: value feature: value ...}.");
            g["BinaryTask"] = new SimplePredicate<object>("BinaryTask",
                o => o is Task { ArgumentCount: 2 })
                .Arguments("x")
                .Documentation("type testing", "Succeeds when its argument is 2-argument task");
            g["Empty"] = Cons.Empty;
            g["EmptyMaxQueue"] = ImmutableSortedSet.Create<(object element, float priority)>(PriorityQueueComparer.Max);
            g["EmptyMinQueue"] = ImmutableSortedSet.Create<(object element, float priority)>(PriorityQueueComparer.Min);

            g["CountAttempts"] = new GeneralPrimitive("CountAttempts", (args, o, bindings, p, k) =>
            {
                ArgumentCountException.Check("CountAttempts", 1, args, o);
                ArgumentInstantiationException.Check("CountAttempts", args[0], false, bindings, args, o);
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

            g["RandomFloat"] = new SimpleFunction<float, float, float>("RandomFloat", Randomization.Float)
                .Arguments("min", "max", "?random")
                .Documentation("randomization", "Sets ?random to a random float such that min <= ?random <= max");

            g["RandomElement"] = new GeneralPredicate<IList, object>(
                "RandomElement",
                (list, elt) => list.Contains(elt),
                list => list.BadShuffle().Cast<object>(),
                null,
                null)
                .Arguments("list", "?element")
                .Documentation("randomization","Sets ?element to a random element of list.  If this is backtracked, it generates a random shuffle of the elements of this list.  However, not all shuffles are possible; it starts with a random element and moves to subsequent elements with a random step size.");

            g["Gaussian"] = new SimpleFunction<float, float, float>("Gaussian", (mean, stdev) =>
                {
                    double a = 1-Randomization.Random.NextDouble();
                    double b = 1-Randomization.Random.NextDouble();
                    var sigma = Math.Sqrt(-2 * Math.Log(a)) * Math.Log(2 * Math.PI * b);
                    return (float)(mean + sigma * stdev);
                })
                .Arguments("mean", "stdev", "random")
                .Documentation("Generates a random, normally distributed floating-point value with the specified mean and standard deviation.");

            g[nameof(SampleFeatures)] = new GeneralPrimitive(nameof(SampleFeatures), SampleFeatures)
                    .Arguments("featureStructure", "?featureName")
                    .Documentation("Given a feature structure of the form { feature:weight ...}, randomly chooses features with probability proportional to the weights.  If backtracked, it will rechoose randomly without repetition (i.e. it performs a weighted shuffle)");
                
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
                (arglist, output) =>
                {
                    ArgumentCountException.CheckAtLeast("EnvironmentOption", 1, arglist, output);
                    var optionName = ArgumentTypeException.Cast<string>("EnvironmentOption", arglist[0], arglist, output);
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

            Documentation.SectionIntroduction("math",
                "Predicates that test the spelling of strings.");

            g[nameof(LinearInterpolate)] = new SimpleFunction<float,object[], object[], float>(
                nameof(LinearInterpolate), LinearInterpolate)
                .Arguments("argument", "[control_point_arguments ...]", "[control_point_values ...]", "?result")
                .Documentation("Evaluates a piecewise-linear function defined by the specified control points at the specified argument and returns its value.");

            Documentation.SectionIntroduction("Declaration groups", "These tasks implement a very limited macro processing facility. They allow the heads of a group of methods to be automatically rewritten, usually by automatically adding arguments to their patterns.");
            Documentation.UserDefinedSystemTask("DeclarationGroup", "[pattern ...]")
                .Documentation("Declaration groups",
                    "User-defined.  True when the specified pattern marks the start of a declaration group.");
            Documentation.UserDefinedSystemTask("DeclarationExpansion", "groupPattern", "original_head", "?expanded_head")
                .Documentation("Declaration groups",
                    "User-defined.  True when a method appearing within a declaration group with the specified pattern should have its head rewritten from the original form to the expanded form.");

            HigherOrderBuiltins.DefineGlobals();
            ReflectionBuiltins.DefineGlobals();
            Documentation.DefineGlobals(Module.Global);
            ElNode.DefineGlobals();
        }

        private static bool HasFeature(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(nameof(HasFeature), 2, args, o);
            var structureArg = args[0];
            if (structureArg is LogicVariable)
                return false;
            var fs = ArgumentTypeException.Cast<FeatureStructure>(nameof(HasFeature), structureArg, args, o);

            var featureArg = args[1];
            if (featureArg is LogicVariable l)
            {
                foreach (var f in fs.FeatureValues(e.Unifications))
                {
                    if (k(o, BindingList.Bind(e.Unifications, l, f.Key.Name), e.State, predecessor))
                        return true;
                }

                return false;
            } else
            {
                var feature = ArgumentTypeException.Cast<string>(nameof(HasFeature), featureArg, args, o);
                return fs.ContainsFeature(feature, e.Unifications) && k(o, e.Unifications, e.State, predecessor);
            }
        }

        private static bool SampleFeatures(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(nameof(SampleFeatures), 2, args, o);
            var fs = ArgumentTypeException.Cast<FeatureStructure>(nameof(SampleFeatures), args[0], args, o);
            var features = fs.FeatureValues(e.Unifications).ToArray();
            var shuffled = features.WeightedShuffle(pair => Convert.ToSingle(pair.Value));
            foreach (var feature in shuffled)
            {
                if (e.Unify(args[1], feature.Key.Name, out var u)
                    && k(o, u, e.State, predecessor))
                    return true;
            }

            return false;
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
                (args, output) =>
                {
                    ArgumentCountException.Check("PathStructure", 3, args, output);
                    if (args[2] is LogicVariable)
                    {
                        // Path argument uninstantiated
                        args[2] = Path.Combine(ArgumentTypeException.Cast<string>("PathStructure", args[0], args, output),
                            ArgumentTypeException.Cast<string>("PathStructure", args[1], args, output));
                        return new[] {args};
                    }
                    else
                    {
                        var path = ArgumentTypeException.Cast<string>("PathStructure", args[2], args, output);
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

        private static bool Throw(object?[] args, TextBuffer output) => ThrowDriver(args, output, false);
        private static bool BailOut(object?[] args, TextBuffer output) => ThrowDriver(args, output, true);

        private static bool ThrowDriver(object?[] args, TextBuffer output, bool suppressStackTrace)
        {
            string[] Stringify(object? o)
            {
                if (o is string[] text)
                    return text;
                if (o == null)
                    return new [] {"null"};
                var s = Writer.TermToString(o);
                if (s != "")
                    return new[] { s };
                return Array.Empty<string>();
            }

            throw new StepExecutionException(args.SelectMany(Stringify).Untokenize(), output, suppressStackTrace);
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

        public static float LinearInterpolate(float arg, object[] controlArgs, object[] controlValues)
            => ((IPiecewiseLinearFunction)new LinearInterpolateWrapper(controlArgs, controlValues)).Evaluate(arg);

        private sealed class LinearInterpolateWrapper : IPiecewiseLinearFunction
        {
            private readonly object[] args;
            private readonly object[] values;

            public LinearInterpolateWrapper(object[] args, object[] values)
            {
                this.args = args;
                this.values = values;
            }

            public int ControlPointCount => args.Length;
            public float ControlPointArgument(int cpIndex) => Convert.ToSingle(args[cpIndex]);

            public float ControlPointValue(int cpIndex)  => Convert.ToSingle(values[cpIndex]);
        }
    }
}
