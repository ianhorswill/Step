﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HigherOrderBuiltins.cs" company="Ian Horswill">
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
using System.Linq;
using Step.Output;
using Step.Utilities;

namespace Step.Interpreter
{
    /// <summary>
    /// Implementations of higher-order builtin primitives.
    /// </summary>
    public static class HigherOrderBuiltins
    {
        /// <summary>
        /// The built-in And task, for any C# code that needs to recognize references to it.
        /// </summary>
        public static readonly Task And = new GeneralPrimitive(nameof(And), AndImplementation)
            .Arguments("calls", "...")
            .Documentation("control flow", "Runs each of the calls, in order.");

        /// <summary>
        /// The built-in Or task, for any C# code that needs to recognize references to it.
        /// </summary>
        public static readonly Task Or = new GeneralPrimitive(nameof(Or), OrImplementation)
            .Arguments("calls", "...")
            .Documentation("control flow", "Runs each of the calls, in order until one works.");

        /// <summary>
        /// The built-in Not task, for any C# code that needs to recognize references to it.
        /// </summary>
        public static readonly Task Not = new GeneralPrimitive(nameof(Not), NotImplementation)
        .Arguments("call")
            .Documentation("higher-order predicates", "Runs call.  If the call succeeds, it Not, fails, undoing any effects of the call.  If the call fails, then Not succeeds.  This requires the call to be ground (not contain any uninstantiated variables), since [Not [P ?x]] means \"not [P ?x] for any ?x\".  Use NotAny if you mean to have unbound variables in the goal.");

        internal static void DefineGlobals()
        {
            var g = Module.Global;

            Documentation.SectionIntroduction("control flow",
                "Tasks that run or otherwise control the execution of other tasks.");

            Documentation.SectionIntroduction("control flow//calling tasks",
                "Tasks that call another task once.");

            Documentation.SectionIntroduction("control flow//looping",
                "Tasks that repeatedly call other tasks.");

            Documentation.SectionIntroduction("control flow//looping//all solutions predicates",
                "Tasks that collect together all the solutions to a given call.");

            Documentation.SectionIntroduction("higher-order predicates",
                "Predicates that run other predicates.");

            g[nameof(Call)] = new GeneralPrimitive(nameof(Call), Call)
                .Arguments("call", "extra_arguments", "...")
                .Documentation("control flow//calling tasks", "Runs the call to the task represented in the tuple 'call'. If extra_arguments are included, they will be added to the end of the call tuple.");
            g[nameof(CallDiscardingStateChanges)] = new GeneralPrimitive(nameof(CallDiscardingStateChanges), CallDiscardingStateChanges)
                .Arguments("call", "extra_arguments", "...")
                .Documentation("control flow//calling tasks", "Runs the call to the task represented in the tuple 'call', but discards any changes to global variables or fluents it makes. If extra_arguments are included, they will be added to the end of the call tuple.");
            g[nameof(IgnoreOutput)] = new GeneralPrimitive(nameof(IgnoreOutput), IgnoreOutput)
                .Arguments("calls", "...")
                .Documentation("control flow//calling tasks", "Runs each of the calls, in order, but throws away their output text.");
            g[nameof(Begin)] = new GeneralPrimitive(nameof(Begin), Begin)
                .Arguments("task", "...")
                .Documentation("control flow", "Runs each of the tasks, in order.");
            g[nameof(And)] = And;
            g[nameof(Or)] = Or;
                g[nameof(Not)] = Not;
            g[nameof(NotAny)] = new GeneralPrimitive(nameof(NotAny), NotAny)
                .Arguments("call")
                .Documentation("higher-order predicates", "Runs call.  If the call succeeds, it Not, fails, undoing any effects of the call.  If the call fails, then Not succeeds.");
            g[nameof(FindAll)] = new GeneralPrimitive(nameof(FindAll), FindAll)
                .Arguments("?result", "call", "?all_results")
                .Documentation("control flow//looping//all solutions predicates", "Runs call, backtracking to find every possible solution to it.  For each solution, FindAll records the value of ?result, and returns a list of all ?results in order, in ?all_results.  If backtracking produces duplicate ?results, there will be multiple copies of them in ?all_results; to eliminate duplicate solutions, use FindUnique.  If call never fails, this will run forever.");
            g[nameof(FindUnique)] = new GeneralPrimitive(nameof(FindUnique), FindUnique)
                .Arguments("?result", "call", "?all_results")
                .Documentation("control flow//looping//all solutions predicates", "Runs call, backtracking to find every possible solution to it.  For each solution, FindUnique records the value of ?result, and returns a list of all ?results in order, in ?all_results, eliminating duplicate solutions.  If call never fails, this will run forever.");
            g[nameof(FindFirstNUnique)] = new GeneralPrimitive(nameof(FindFirstNUnique), FindFirstNUnique)
                .Arguments("n", "?result", "call", "?all_results")
                .Documentation("control flow//looping//all solutions predicates", "Like FindUnique, but takes only the first n unique solutions that are generated.  Fails if there are fewer than n unique solutions.");
            g[nameof(FindAtMostNUnique)] = new GeneralPrimitive(nameof(FindAtMostNUnique), FindAtMostNUnique)
                .Arguments("n", "?result", "call", "?all_results")
                .Documentation("control flow//looping//all solutions predicates", "Like FindUnique, but takes only the first n unique solutions that are generated.");
            g[nameof(DoAll)] = new DeterministicTextGeneratorMetaTask(nameof(DoAll), DoAll)
                .Arguments("generator_call", "other_calls", "...")
                .Documentation("control flow//looping", "Runs generator_call, finding all its solutions by backtracking.  For each solution, runs the other tasks, collecting all their text output.  Since the results are backtracked, any variable bindings or set commands are undone.");
            g[nameof(AccumulateOutput)] = new GeneralPrimitive(nameof(AccumulateOutput), AccumulateOutput)
                .Arguments("generator", "printer")
                .Documentation("control flow//looping", "Runs generator, finding all its solutions by backtracking.  For each solution, runs printer, accumulating their text output and changes to global variables.");
            g[nameof(AccumulateOutputWithSeparators)] = new GeneralPrimitive(nameof(AccumulateOutputWithSeparators), AccumulateOutputWithSeparators)
                .Arguments("generator", "printer", "separator", "lastSeparator")
                .Documentation("control flow//looping", "Runs generator, finding all its solutions by backtracking.  For each solution, runs printer, accumulating their text output and changes to global variables.  Between output from different solutions, it inserts the string separator, or lastSeparator if it is the last solution.");
            g[nameof(ForEach)] = new GeneralPrimitive(nameof(ForEach), ForEach)
                .Arguments("generator_call", "other_calls", "...")
                .Documentation("control flow//looping", "Runs generator_call, finding all its solutions by backtracking.  For each solution, runs the other tasks, collecting all their text output.  Since the results are backtracked, any variable bindings are undone.  However, all text generated and set commands performed are preserved.");
            g[nameof(Implies)] = new GeneralPrimitive(nameof(Implies), Implies)
                .Arguments("higher-order predicates", "other_calls", "...")
                .Documentation("higher-order predicates", "True if for every solution to generator_call, other_calls succeeds.  So this is essentially like ForEach, but whereas ForEach always succeeds, Implies fails if other_calls ever fails.  Text output and sets of global variables are preserved, as with ForEach.");
            g[nameof(Once)] = new GeneralPrimitive(nameof(Once), Once)
                .Arguments("code", "...")
                .Documentation("control flow//controlling backtracking", "Runs code normally, however, if any subsequent code backtracks, once will not rerun the code, but will fail to whatever code preceded it.");
            g[nameof(ExactlyOnce)] = new GeneralPrimitive(nameof(ExactlyOnce), ExactlyOnce)
                .Arguments("code", "...")
                .Documentation("control flow//controlling backtracking", "Runs code normally.  If the code fails, ExactlyOnce throws an exception.  If it succeeds, ExactlyOnce succeeds.  However, if any subsequent code backtracks, once will not rerun the code, but will fail to whatever code preceded it.");
            g[nameof(Max)] = new GeneralPrimitive(nameof(Max), Max)
                .Arguments("?scoreVariable", "code", "...")
                .Documentation("control flow//looping//all solutions predicates", "Runs code, backtracking to find all solutions, keeping the state (text output and variable bindings) of the solution with the largest value of ?scoreVariable");
            g[nameof(Min)] = new GeneralPrimitive(nameof(Min), Min)
                .Arguments("?scoreVariable", "code", "...")
                .Documentation("control flow//looping//all solutions predicates", "Runs code, backtracking to find all solutions, keeping the state (text output and variable bindings) of the solution with the smallest value of ?scoreVariable");
            g[nameof(SaveText)] = new GeneralPrimitive(nameof(SaveText), SaveText)
                .Arguments("call", "?variable")
                .Documentation("control flow//calling tasks", "Runs call, but places its output in ?variable rather than the output buffer.  Does not preserve variable bindings, output, or state changes, other than saving the output in the second argument.");
            g[nameof(PreviousCall)] = new GeneralPrimitive(nameof(PreviousCall), PreviousCall)
                .Arguments("?call_pattern")
                .Documentation("reflection//dynamic analysis", "Unifies ?call_pattern with the most recent successful call that matches it.  Backtracking will match against previous calls.");
            g[nameof(UniqueCall)] = new GeneralPrimitive(nameof(UniqueCall), UniqueCall)
                .Arguments("?call_pattern")
                .Documentation("reflection//dynamic analysis", "Calls ?call_pattern, finding successive solutions until one is found that can't be unified with a previous successful call.");
            g[nameof(Parse)] = new GeneralPrimitive(nameof(Parse), Parse)
                .Arguments("call", "text")
                .Documentation("control flow//calling tasks", "True if call can generate text as its output.  This is done by running call and backtracking whenever its output diverges from text.  Used to determine if a grammar can generate a given string.");

            g[nameof(TreeSearch)] = new GeneralPrimitive(nameof(TreeSearch), TreeSearch)
                .Arguments("startNode", "finalNode", "utility", "NextNode", "GoalNode", "NodeUtility")
                .Documentation("Performs a best-first search of a tree starting at startNode, using NextNode to enumerate neighbors of a given node, GoalNode to test whether a node is a goal node, and NodeUtility to compute a utility to use for the best-first search.");
        }

        private static bool Call(object?[] args, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.CheckAtLeast(nameof(Call), 1, args, output);
            var call = ArgumentTypeException.Cast<object[]>(nameof(Call), args[0], args, output);

            if (!(call[0] is Task task))
                throw new InvalidOperationException(
                    "Task argument to Call must be a task");

            var taskArgs = new object?[call.Length - 1 + args.Length - 1];

            var i = 0;
            for (var callIndex = 1; callIndex < call.Length; callIndex++)
                taskArgs[i++] = call[callIndex];
            for (var argsIndex = 1; argsIndex < args.Length; argsIndex++)
                taskArgs[i++] = args[argsIndex];

            return task.Call(taskArgs, output, env, predecessor, k);
        }

        private static bool CallDiscardingStateChanges(object?[] args, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.CheckAtLeast(nameof(CallDiscardingStateChanges), 1, args, output);
            var call = ArgumentTypeException.Cast<object[]>(nameof(Call), args[0], args, output);

            if (!(call[0] is Task task))
                throw new InvalidOperationException(
                    "Task argument to CallDiscardingStateChanges must be a task");

            var taskArgs = new object?[call.Length - 1 + args.Length - 1];

            var i = 0;
            for (var callIndex = 1; callIndex < call.Length; callIndex++)
                taskArgs[i++] = call[callIndex];
            for (var argsIndex = 1; argsIndex < args.Length; argsIndex++)
                taskArgs[i++] = args[argsIndex];

            return task.Call(taskArgs, output, env, predecessor,
                (newOutput, newEnvironment, newState, newFrame) =>
                    k(newOutput, newEnvironment, env.State, newFrame));
        }

        private static bool Eval(object?[] call, TextBuffer output, BindingEnvironment env, MethodCallFrame? predecessor,
            Step.Continuation k, string taskName)
        {
            if (!(call[0] is Task task))
                throw new InvalidOperationException(
                    $"Task argument to {taskName} must be a task: {Writer.TermToString(call[0])}");

            return task.Call(call.Skip(1).ToArray(), output, env, predecessor, k);
        }

        private static bool Begin(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
            => Step.Try(Step.ChainFromBody("Begin", args), o, e, k, predecessor);

        private static bool AndImplementation(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
            => Step.Try(Step.ChainFromBody("AndImplementation", args), o, e, k, predecessor);

        private static bool OrImplementation(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
            => args.Any(call =>
            {
                var tuple = ArgumentTypeException.Cast<object[]>("OrImplementation", call, args, o);
                return Eval(tuple, o, e, predecessor, k, "OrImplementation");
            });

        private static bool IgnoreOutput(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
        {
            return Step.Try(Step.ChainFromBody("IgnoreOutput", args),
                o, e,
                (_, u,s, p) => k(o, u, s, p),
                predecessor);
        }

        private static bool NotImplementation(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
        {
            foreach (var arg in args)
                if (!Term.IsGround(arg))
                    throw new ArgumentInstantiationException("Not", e, args, "Use NotAny if you intend goals that aren't ground.", o);
            // Whether the call to args below succeeded
            var success = false;
            
            // This always fails, since its continuation fails too
            Step.Try(Step.ChainFromBody("Not", args),
                o, e,
                    (newOut, newE, newK, newP) =>
                    {
                        // Remember that we succeeded, then fail
                        success = true;
                        return false;
                    },
                    predecessor);

            // If the call to args succeeded, fail; otherwise call continuation
            return !success && k(o, e.Unifications, e.State, predecessor);
        }

        private static bool NotAny(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
        {
            // Whether the call to args below succeeded
            var success = false;

            // This always fails, since its continuation fails too
            Step.Try(Step.ChainFromBody("NotAny", args),
                o, e,
                    (newOut, newE, newK, newP) =>
                    {
                        // Remember that we succeeded, then fail
                        success = true;
                        return false;
                    },
                    predecessor);

            // If the call to args succeeded, fail; otherwise call continuation
            return !success && k(o, e.Unifications, e.State, predecessor);
        }

        private static IEnumerable<string> DoAll(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor)
        {
            var t = AllSolutionTextFromBody("DoAll", args, o, e, predecessor);
            return t.SelectMany(strings => strings);
        }

        private static bool AccumulateOutput(object?[] args, TextBuffer o, BindingEnvironment e,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(nameof(AccumulateOutput), 2, args, o);
            var generator = ArgumentTypeException.Cast<object[]>(nameof(AccumulateOutput), args[0], args, o);

            var (text, state) = AccumulateEffectsFromGenerator(nameof(AccumulateOutput), generator, args[1], o, e, predecessor);
            return k(text, e.Unifications, state, predecessor);
        }

        private static bool AccumulateOutputWithSeparators(object?[] args, TextBuffer o, BindingEnvironment e,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(nameof(AccumulateOutput), 4, args, o);
            var generator = ArgumentTypeException.Cast<object[]>(nameof(AccumulateOutput), args[0], args, o);
            var separator = ArgumentTypeException.Cast<string[]>(nameof(AccumulateOutput), args[2], args, o);
            var terminator = ArgumentTypeException.Cast<string[]>(nameof(AccumulateOutput), args[3], args, o);

            var (text, state) = AccumulateEffectsFromGenerator(separator, terminator, nameof(AccumulateOutput), generator, args[1], o, e, predecessor);
            return k(text, e.Unifications, state, predecessor);
        }

        private static bool FindAll(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check("FindAll", 3, args, o);
            var solution = args[0];
            var call = args[1] as object[];
            if (call == null || call.Length == 0)
                throw new ArgumentException("Invalid goal expression");
            var task = ArgumentTypeException.Cast<Task>("FindAll", call[0], args, o);
            var taskArgs = call.Skip(1).ToArray();

            var result = args[2];
            var resultList = new List<object?>();

            task.Call(taskArgs, o, e, predecessor, (newO, u, s, p) =>
            {
                resultList.Add(e.Resolve(solution, u));
                return false;
            });
            return e.Unify(result, resultList.ToArray(), out var final)
                   && k(o, final, e.State, predecessor);
        }

        private static bool FindUnique(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check("FindUnique", 3, args, o);
            var solution = args[0];
            var call = args[1] as object[];
            if (call == null || call.Length == 0)
            {
                StepThread.ErrorText(o);
                throw new ArgumentException("Invalid goal expression");
            }
            var task = ArgumentTypeException.Cast<Task>("FindUnique", call[0], args, o);
            var taskArgs = call.Skip(1).ToArray();

            var result = args[2];
            return FindUniqueDriver(null, false, o, e, predecessor, k, task, taskArgs, solution, result);
        }

        private static bool FindFirstNUnique(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(nameof(FindFirstNUnique), 4, args, o);
            var count = ArgumentTypeException.Cast<int>(nameof(FindFirstNUnique), args[0], args, o);
            var solution = args[1];
            var call = args[2] as object[];
            if (call == null || call.Length == 0)
            {
                StepThread.ErrorText(o);
                throw new ArgumentException("Invalid goal expression");
            }
            var task = ArgumentTypeException.Cast<Task>(nameof(FindFirstNUnique), call[0], args, o);
            var taskArgs = call.Skip(1).ToArray();

            var result = args[3];
            return FindUniqueDriver(count, true, o, e, predecessor, k, task, taskArgs, solution, result);
        }

        private static bool FindAtMostNUnique(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(nameof(FindAtMostNUnique), 4, args, o);
            var count = ArgumentTypeException.Cast<int>(nameof(FindAtMostNUnique), args[0], args, o);
            var solution = args[1];
            var call = args[2] as object[];
            if (call == null || call.Length == 0)
            {
                StepThread.ErrorText(o);
                throw new ArgumentException("Invalid goal expression");
            }
            var task = ArgumentTypeException.Cast<Task>(nameof(FindAtMostNUnique), call[0], args, o);
            var taskArgs = call.Skip(1).ToArray();

            var result = args[3];
            return FindUniqueDriver(count, false, o, e, predecessor, k, task, taskArgs, solution, result);
        }

        private static bool FindUniqueDriver(int? solutionCount, bool failIfInsufficient, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k,
            Task task, object?[] taskArgs, object? solution, object? result)
        {
            var resultSet = new List<object?>();

            task.Call(taskArgs, o, e, predecessor, (newO, u, s, p) =>
            {
                object? r = e.Resolve(solution, u);
                if (resultSet.All(elt => !Term.LiterallyEqual(elt, r)))
                    resultSet.Add((r));
                return solutionCount.HasValue && resultSet.Count == solutionCount.Value;
            });
            return (!failIfInsufficient || !solutionCount.HasValue || solutionCount.Value == resultSet.Count)
                   && e.Unify(result, resultSet.ToArray(), out var final)
                   && k(o, final, e.State, predecessor);
        }

        private static bool ForEach(object?[] args, TextBuffer output, BindingEnvironment env, MethodCallFrame? predecessor, Step.Continuation k)
        {
            if (args.Length < 2)
                throw new ArgumentCountException("ForEach", 2, args, output);

            var producer = args[0];
            var producerChain = Step.ChainFromBody("ForEach", producer);
            var consumer = args.Skip(1).ToArray();
            var consumerChain = Step.ChainFromBody("ForEach", consumer);

            var dynamicState = env.State;
            var resultOutput = output;

            Step.Try(producerChain, resultOutput, env,
                (o, u, d, p) =>
                {
                    // We've got a solution to the producer in u.
                    // So run the consumer once with u but not d or o.
                    Step.Try(consumerChain, resultOutput,
                        new BindingEnvironment(env, u, dynamicState),
                        (o2, u2, d2, newP) =>
                        {
                            // Save modifications to dynamic state, output; throw away binding state
                            dynamicState = d2;
                            resultOutput = o2;
                            // Accept this one solution to consumer; don't backtrack it.
                            return true;
                        },
                        p);
                    // Backtrack to generate the next solution for producer
                    return false;
                },
                predecessor);

            // Use original unifications but accumulated output and state.
            return k(resultOutput, env.Unifications, dynamicState, predecessor);
        }

        private static bool Implies(object?[] args, TextBuffer output, BindingEnvironment env, MethodCallFrame? predecessor, Step.Continuation k)
        {
            if (args.Length < 2)
                throw new ArgumentCountException(nameof(Implies), 2, args, output);

            var producer = args[0];
            var producerChain = Step.ChainFromBody(nameof(Implies), producer);
            var consumer = args.Skip(1).ToArray();
            var consumerChain = Step.ChainFromBody(nameof(Implies), consumer);

            var dynamicState = env.State;
            var resultOutput = output;
            var allTrue = true;
            Step.Try(producerChain, resultOutput, env,
                (o, u, d, p) =>
                {
                    // We've got a solution to the producer in u.
                    // So run the consumer once with u but not d or o.
                    allTrue &= Step.Try(consumerChain, resultOutput,
                        new BindingEnvironment(env, u, dynamicState),
                        (o2, u2, d2, newP) =>
                        {
                            // Save modifications to dynamic state, output; throw away binding state
                            dynamicState = d2;
                            resultOutput = o2;
                            // Accept this one solution to consumer; don't backtrack it.
                            return true;
                        },
                        p);
                    // Backtrack to generate the next solution for producer
                    return false;
                },
                predecessor);

            // Use original unifications but accumulated output and state.
            return allTrue && k(resultOutput, env.Unifications, dynamicState, predecessor);
        }

        private static bool Once(object?[] args, TextBuffer output, BindingEnvironment env, MethodCallFrame? predecessor, Step.Continuation k)
        {
            TextBuffer finalOutput = output;
            BindingList? finalBindings = null;
            State finalState = State.Empty;
            MethodCallFrame? finalFrame = predecessor;
            bool success = false;

            GenerateSolutionsFromBody("Once", args, output, env,
                (o, u, d, p) =>
                {
                    success = true;
                    finalOutput = o;
                    finalBindings = u;
                    finalState = d;
                    finalFrame = p;
                    return true;
                },
                predecessor);

            return success && k(finalOutput, finalBindings, finalState, finalFrame);
        }

        private static bool ExactlyOnce(object?[] args, TextBuffer output, BindingEnvironment env, MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check("ExactlyOnce", 1, args, output);
            TextBuffer finalOutput = output;
            BindingList? finalBindings = null;
            State finalState = State.Empty;
            MethodCallFrame? finalFrame = predecessor;
            bool failure = true;

            var chain = Step.ChainFromBody("ExactlyOnce", args);
            Step.Try(chain, output, env,
                (o, u, d, p) =>
                {
                    failure = false;
                    finalOutput = o;
                    finalBindings = u;
                    finalState = d;
                    finalFrame = p;
                    return true;
                },
                predecessor);

            if (failure)
            {
                var failedCall = (Call?)chain;
                throw new CallFailedException(env.Resolve(failedCall!.Task), env.ResolveList(failedCall.Arglist), output);
            }
            return k(finalOutput, finalBindings, finalState, finalFrame);
        }

        private static bool Max(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
        {
            return MaxMinDriver("Max", args, 1, o, e, k, predecessor);
        }

        private static bool Min(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
        {
            return MaxMinDriver("Min", args, -1, o, e, k, predecessor);
        }

        /// <summary>
        /// Core implementation of both Max and Min
        /// </summary>
        private static bool MaxMinDriver(string taskName, object?[] args,
            int multiplier, TextBuffer o, BindingEnvironment e,
            Step.Continuation k,
            MethodCallFrame? predecessor)
        {
            var scoreVar = args[0] as LogicVariable;
            if (scoreVar == null)
                throw new ArgumentInstantiationException(taskName, e, args, o);

            var bestScore = multiplier * float.NegativeInfinity;
            var bestFrame = predecessor;
            CapturedState bestResult = new CapturedState();
            var gotOne = false;

            GenerateSolutionsFromBody(taskName, args.Skip(1).ToArray(), o, e,
                (output, u, d, p) =>
                {
                    gotOne = true;

                    var env = new BindingEnvironment(e, u, d);

                    var maybeScore = env.Resolve(scoreVar);
                    float score;
                    switch (maybeScore)
                    {
                        case int i:
                            score = i;
                            break;

                        case float f:
                            score = f;
                            break;

                        case double df:
                            score = (float) df;
                            break;

                        case LogicVariable _:
                            throw new ArgumentInstantiationException(taskName, new BindingEnvironment(e, u, d), args, o);

                        default:
                            throw new ArgumentTypeException(taskName, typeof(float), maybeScore, args, o);
                    }

                    if (multiplier * score > multiplier * bestScore)
                    {
                        bestScore = score;
                        bestResult = new CapturedState(o, output, u, d);
                        bestFrame = p;
                    }

                    // Always ask for another solution
                    return false;
                },
                predecessor);

            // When we get here, we've iterated through all solutions and kept the best one.
            // So pass it on to our continuation
            return gotOne
                   && k(o.Append(bestResult.Output!), bestResult.Bindings, bestResult.State, bestFrame);
        }

        private static bool SaveText(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check("SaveText", 2, args, o);

            var textVar = e.Resolve(args[1]);
            if (textVar == null)
                throw new ArgumentInstantiationException("SaveText", e, args, o);

            var invocation = args[0] as object[];
            if (invocation == null || invocation.Length == 0)
                throw new ArgumentTypeException("SaveText", typeof(Call), args[0], args, o);
            var arglist = new object[invocation.Length - 1];
            Array.Copy(invocation, 1, arglist, 0, arglist.Length);
            var call = new Call(invocation[0], arglist, null);
            var initialLength = o.Length;
            string[]? chunk = null;
            var frame = predecessor;

            if (call.Try(o, e,
                    (output, b, d, p) =>
                    {
                        frame = p;
                        chunk = new string[output.Length - initialLength];
                        Array.Copy(o.Buffer, initialLength, chunk, 0, output.Length - initialLength);
                        return true;
                    },
                    predecessor)
                && e.Unify(textVar, chunk, e.Unifications, out var newUnifications))
                return k(o, newUnifications, e.State, frame);

            return false;
        }

        private static bool Parse(object?[] args, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.CheckAtLeast(nameof(Parse), 2, args, output);
            var call = ArgumentTypeException.Cast<object[]>(nameof(Parse),env.Resolve(args[0]), args, output);
            var text = ArgumentTypeException.Cast<string[]>(nameof(Parse), env.Resolve(args[1]), args, output);

            if (!(call[0] is Task task))
                throw new InvalidOperationException("Task argument to Parse must be a task.");

            var taskArgs = new object[call.Length - 1];

            var i = 0;
            for (var callIndex = 1; callIndex < call.Length; callIndex++)
                taskArgs[i++] = call[callIndex];

            var parseBuffer = TextBuffer.MakeReadModeTextBuffer(text);
            return task.Call(taskArgs, parseBuffer, env, predecessor,
                (buffer, u, s, p) => buffer.ReadCompleted && k(output, u, s, p));
        }

        private static bool TreeSearch(object?[] args, TextBuffer o, BindingEnvironment e,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            var startNode = args[0];
            var nodeVar = args[1] as LogicVariable;
            if (nodeVar == null)
                throw new ArgumentInstantiationException("Search", e, args, o);
            var utilityVar = args[2];
            var next = ArgumentTypeException.Cast<Task>("Search", args[3], args, o);
            var done = ArgumentTypeException.Cast<Task>("Search", args[4], args, o);
            var utility = ArgumentTypeException.Cast<Task>("Search", args[5], args, o);
            var queue = new List<(float priority, object? node, string[] text, BindingEnvironment env, MethodCallFrame? pred)>();

            (bool isGoal, float utility) TestNode(object? node, BindingEnvironment env, MethodCallFrame? p)
            {
                float u=0;
                var success = done.Call(new[] { node }, o, env, p, Step.SucceedContinuation);
                var utilityArgs = new[] { node, utilityVar };
                if (!utility.Call(utilityArgs, o, env, p, (_, unif, s, p2) =>
                    {
                        var util = BindingEnvironment.Deref(utilityVar, unif);
                        u = Convert.ToSingle(util);
                        return true;
                    }))
                    throw new CallFailedException(utility, utilityArgs, o);
                return (success, u);
            }

            bool GenerateResult(object? node, float goalNodeUtility, TextBuffer output, BindingEnvironment env, MethodCallFrame? p) =>
                env.UnifyArrays(new[] { node, goalNodeUtility }, new[] { nodeVar, utilityVar },
                    out BindingList? unif)
                && k(output, unif, env.State, p);

            int Compare((float priority, object? node, string[] text, BindingEnvironment env, MethodCallFrame? pred) x,
                (float priority, object? node, string[] text, BindingEnvironment env, MethodCallFrame? pred) y)
            {
                return x.priority.CompareTo(y.priority);
            }

            var startEvaluation = TestNode(startNode, e, predecessor);
            if (startEvaluation.isGoal && GenerateResult(startNode, startEvaluation.utility, o, e, predecessor))
                return true;

            var solutionAccepted = false; // when true, out continuation has accepted a solution
            queue.Add((startEvaluation.utility, startNode, Array.Empty<string>(), e, predecessor));
            while (!solutionAccepted && queue.Count > 0)
            {
                queue.Sort(Compare);
                var current = queue[^1];
                queue.RemoveAt(queue.Count-1);
                var node = current.node;
                var nextVar = new LogicVariable(nodeVar.Name);
                var nextArgs = new[] { node, nextVar };
                // Get all neighbors of node
                // This is a loop because the continuation fails, forcing it to ask for new solutions
                next.Call(nextArgs, o.Append(current.text), current.env, current.pred, (newText, unif, state, p) =>
                {
                    var nextNode = BindingEnvironment.Deref(nextVar, unif);
                    var nextEnv = new BindingEnvironment(current.env, unif, state);
                    var evaluation = TestNode(nextNode, nextEnv, p);
                    if (evaluation.isGoal && GenerateResult(nextNode, evaluation.utility, newText, nextEnv, p))
                    {
                        solutionAccepted = true;
                        return true; // end call to next
                    }

                    queue.Add((evaluation.utility, nextNode, newText - o, nextEnv, p));
                    return false; // ask for another solution
                });
            }
            return solutionAccepted;
        }

        #region Data structures for recording execution state
        /// <summary>
        /// Used to record the results of a call so those results can be reapplied later.
        /// Used for all-solutions and maximization meta-predicates
        /// </summary>
        private readonly struct CapturedState
        {
            public readonly string[] Output;
            public readonly BindingList? Bindings;
            public readonly State State;

            //private static readonly string[] EmptyOutput = new string[0];

            private CapturedState(string[] output, BindingList? bindings, State state)
            {
                Output = output;
                Bindings = bindings;
                State = state;
            }

            public CapturedState(TextBuffer before, TextBuffer after, BindingList? bindings,
                State state)
                : this(TextBuffer.Difference(before, after), bindings, state)
            { }
        }
        #endregion

        #region Utilities for higher-order primitives
        /// <summary>
        /// Calls a task with the specified arguments and allows the user to provide their own continuation.
        /// The only (?) use case for this is when you want to forcibly generate multiple solutions
        /// </summary>
        internal static void GenerateSolutions(string taskName, object[] args, TextBuffer o, BindingEnvironment e, Step.Continuation k, MethodCallFrame? predecessor)
        {
            new Call(StateVariableName.Named(taskName), args, null).Try(o, e, k, predecessor);
        }

        /// <summary>
        /// Find all solutions to the specified task and arguments.  Return a list of the text outputs of each solution.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        internal static List<string[]> AllSolutionText(string taskName, object[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor)
        {
            var results = new List<string[]>();
            var initialLength = o.Length;
            GenerateSolutions(taskName, args, o, e,
                (output, b, d, p) =>
                {
                    var chunk = new string[output.Length - initialLength];
                    for (var i = initialLength; i < output.Length; i++)
                        chunk[i - initialLength] = o.Buffer[i];
                    results.Add(chunk);
                    return false;
                },
                predecessor);
            return results;
        }

        /// <summary>
        /// Calls all the tasks in the body and allows the user to provide their own continuation.
        /// The only (?) use case for this is when you want to forcibly generate multiple solutions
        /// </summary>
        internal static void GenerateSolutionsFromBody(string callingTaskName, object?[] body, TextBuffer o, BindingEnvironment e,
            Step.Continuation k,
            MethodCallFrame? predecessor)
        {
            Step.Try(Step.ChainFromBody(callingTaskName, body), o, e, k, predecessor);
        }

        /// <summary>
        /// Find all solutions to the specified sequence of calls.  Return a list of the text outputs of each solution.
        /// </summary>
        internal static List<string[]> AllSolutionTextFromBody(string callingTaskName, object?[] body, TextBuffer o,
            BindingEnvironment e, MethodCallFrame? predecessor)
        {
            var results = new List<string[]>();
            var initialLength = o.Length;
            GenerateSolutionsFromBody(callingTaskName, body, o, e,
                (output, b, d, p) =>
                {
                    var chunk = new string[output.Length - initialLength];
                    for (var i = initialLength; i < output.Length; i++)
                        chunk[i - initialLength] = o.Buffer[i];
                    results.Add(chunk);
                    return false;
                },
                predecessor);
            return results;
        }

        internal static (TextBuffer, State) AccumulateEffectsFromGenerator(string callingTask, object[] generator, object? call,
            TextBuffer o, BindingEnvironment e,
            MethodCallFrame? predecessor)
        {
            var gTask = ArgumentTypeException.Cast<Task>(callingTask, generator[0], generator, o);
            var gArgs = generator.Skip(1).ToArray();

            var state = e.State;
            var text = o;

            gTask.Call(gArgs, o, e, predecessor,
                (tb, b, s, p) =>
                {
                    var newCall = ArgumentTypeException.Cast<object?[]>(
                        callingTask,
                        new BindingEnvironment(e, b, s).Resolve(call),
                        null, tb);

                    var cTask = ArgumentTypeException.Cast<Task>(callingTask, newCall[0], newCall, o);
                    var cArgs = newCall.Skip(1).ToArray();

                    cTask.Call(cArgs, text, new BindingEnvironment(e, b, state), p,
                        (output, _3, d, _4) =>
                        {
                            text = output;
                            state = d;
                            return true;
                        });
                    return false;
                });
            return (text, state);
        }

        internal static (TextBuffer, State) AccumulateEffectsFromGenerator(string[] separator, string[] terminator, string callingTask, object[] generator, object? call,
            TextBuffer o, BindingEnvironment e,
            MethodCallFrame? predecessor)
        {
            var gTask = ArgumentTypeException.Cast<Task>(callingTask, generator[0], generator, o);
            var gArgs = generator.Skip(1).ToArray();
            
            var state = e.State;
            var text = o;
            var buffer = new TextBuffer(10);
            var lastText = buffer;
            var count = 0;
            gTask.Call(gArgs, o, e, predecessor,
                (tb, b, s, p) =>
                {
                    count++;
                    // Add the text from the last time around
                    if (count > 2)
                        text = text.Append(separator);
                    if (count > 1) 
                        text = text.Append(lastText);

                    var newCall = ArgumentTypeException.Cast<object?[]>(
                        callingTask,
                        new BindingEnvironment(e, b, s).Resolve(call),
                        null, tb);
                    var cTask = ArgumentTypeException.Cast<Task>(callingTask, newCall[0], null, o);
                    var cArgs = newCall.Skip(1).ToArray();

                    cTask.Call(cArgs, buffer, new BindingEnvironment(e, b, state), p,
                        (output, _3, d, _4) =>
                        {
                            lastText = output;
                            state = d;
                            return true;
                        });
                    return false;
                });
            if (count > 0)
            {
                if (count > 1)
                    text = text.Append(terminator);
                text = text.Append(lastText);
            }
            return (text, state);
        }

        private static bool PreviousCall(object?[] args, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(nameof(PreviousCall), 1, args, output);
            if (args[0] is LogicVariable)
            {
                // [PreviousCall ?var]
                foreach (var priorGoal in MethodCallFrame.GoalChain(predecessor))
                {
                    var e = priorGoal.CallExpression;
                    if (env.Unify(args[0], e, out BindingList? unifications)
                        && k(output, unifications, env.State, predecessor))
                        return true;
                }

                return false;
            }

            // [PreviousCall [Task ?args]]
            var call = ArgumentTypeException.Cast<object?[]>(nameof(PreviousCall), args[0], args, output);
            foreach (var priorGoal in MethodCallFrame.GoalChain(predecessor))
            {
                if (priorGoal.Method == null || priorGoal.Method.Task != call[0])
                    // Don't bother making the call expression and trying to unify.
                    continue;

                var e = priorGoal.CallExpression;
                if (call.Length == e.Length
                    && env.UnifyArrays(call, e, out BindingList? unifications)
                    && k(output, unifications, env.State, predecessor))
                    return true;
            }

            return false;
        }

        private static bool UniqueCall(object?[] args, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.CheckAtLeast(nameof(UniqueCall), 1, args, output);
            var call = ArgumentTypeException.Cast<object?[]>(nameof(PreviousCall), args[0], args, output);
            if (!(call[0] is Task task))
                throw new InvalidOperationException(
                    "Task argument to UniqueCall must be a task.");
            
            var taskArgs = new object?[call.Length - 1 + args.Length - 1];

            var i = 0;
            for (var callIndex = 1; callIndex < call.Length; callIndex++)
                taskArgs[i++] = call[callIndex];
            for (var argsIndex = 1; argsIndex < args.Length; argsIndex++)
                taskArgs[i++] = args[argsIndex];

            var fullCall = call;
            if (args.Length > 1)
            {
                fullCall = new object[taskArgs.Length + 1];
                fullCall[0] = task;
                for (var j = 0; j < taskArgs.Length; j++)
                    fullCall[j + 1] = taskArgs[j];
            }

            if (task.Call(taskArgs, output, env, predecessor,
                (o, u, s, newPredecessor) =>
                {
                    foreach (var priorGoal in MethodCallFrame.GoalChain(predecessor))
                    {
                        if (priorGoal.Method == null || priorGoal.Method.Task != task)
                            // Don't bother making the call expression and trying to unify.
                            continue;

                        if (env.Unify(fullCall, priorGoal.CallExpression, u, out BindingList? _))
                            // We already did a call that matches this call
                            // So have the continuation return false, forcing the task.Call above to backtrack
                            // and try to generate a new solution
                            return false;
                    }

                    return k(o, u, s, newPredecessor);
                }))
                return true;
            
            return false;
        }
        #endregion
    }
}
