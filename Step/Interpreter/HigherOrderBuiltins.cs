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
using static Step.Interpreter.PrimitiveTask;

namespace Step.Interpreter
{
    /// <summary>
    /// Implementations of higher-order builtin primitives.
    /// </summary>
    internal static class HigherOrderBuiltins
    {
        internal static void DefineGlobals()
        {
            var g = Module.Global;

            g[nameof(Call)] = (MetaTask)Call;
            g[nameof(Begin)] = (MetaTask)Begin;
            g[nameof(IgnoreOutput)] = (MetaTask)IgnoreOutput;
            g[nameof(Not)] = (MetaTask) Not;
            g[nameof(DoAll)] = (DeterministicTextGeneratorMetaTask) DoAll;
            g[nameof(ForEach)] = (MetaTask) ForEach;
            g[nameof(Once)] = (MetaTask) Once;
            g[nameof(ExactlyOnce)] = (MetaTask) ExactlyOnce;
            g[nameof(Max)] = (MetaTask) Max;
            g[nameof(Min)] = (MetaTask) Min;
            g[nameof(SaveText)] = (MetaTask) SaveText;
            g[nameof(PreviousCall)] = (MetaTask) PreviousCall;
            g[nameof(UniqueCall)] = (MetaTask)UniqueCall;
            g[nameof(Parse)] = (MetaTask)Parse;
        }

        private static bool Call(object[] args, TextBuffer output, BindingEnvironment env,
            Step.Continuation k, MethodCallFrame predecessor)
        {
            ArgumentCountException.CheckAtLeast(nameof(Call), 1, args);
            var call = ArgumentTypeException.Cast<object[]>(nameof(Call), args[0], args);

            var task = call[0] as CompoundTask;
            if (task == null)
                throw new InvalidOperationException(
                    "Task argument to Call must be a compound task, i.e. a user-defined task with methods.");

            var taskArgs = new object[call.Length - 1 + args.Length - 1];

            var i = 0;
            for (var callIndex = 1; callIndex < call.Length; callIndex++)
                taskArgs[i++] = call[callIndex];
            for (var argsIndex = 1; argsIndex < args.Length; argsIndex++)
                taskArgs[i++] = args[argsIndex];

            return task.Call(taskArgs, output, env, predecessor, k);
        }

        private static bool Begin(object[] args, TextBuffer o, BindingEnvironment e, Step.Continuation k, MethodCallFrame predecessor)
        {
            return Step.ChainFromBody("Begin", args).Try(o, e, k, predecessor);
        }

        private static bool IgnoreOutput(object[] args, TextBuffer o, BindingEnvironment e, Step.Continuation k, MethodCallFrame predecessor)
        {
            return Step.ChainFromBody("IgnoreOutput", args).Try(
                o, e,
                (_, u,s, p) => k(o, u, s, p),
                predecessor);
        }

        private static bool Not(object[] args, TextBuffer o, BindingEnvironment e, Step.Continuation k, MethodCallFrame predecessor)
        {
            // Whether the call to args below succeeded
            var success = false;
            
            // This always fails, since its continuation fails too
            Step.ChainFromBody("Not", args)
                .Try(o, e,
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
        
        private static IEnumerable<string> DoAll(object[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame predecessor) 
            => AllSolutionTextFromBody("DoAll", args, o, e, predecessor).SelectMany(strings => strings);

        private static bool ForEach(object[] args, TextBuffer output, BindingEnvironment env, Step.Continuation k, MethodCallFrame predecessor)
        {
            if (args.Length < 2)
                throw new ArgumentCountException("ForEach", 2, args);

            var producer = args[0];
            var producerChain = Step.ChainFromBody("ForEach", producer);
            var consumer = args.Skip(1).ToArray();
            var consumerChain = Step.ChainFromBody("ForEach", consumer);

            var dynamicState = env.State;
            var resultOutput = output;

            producerChain.Try(resultOutput, env,
                (o, u, d, p) =>
                {
                    // We've got a solution to the producer in u.
                    // So run the consumer once with u but not d or o.
                    consumerChain.Try(resultOutput,
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

        private static bool Once(object[] args, TextBuffer output, BindingEnvironment env, Step.Continuation k, MethodCallFrame predecessor)
        {
            TextBuffer finalOutput = output;
            BindingList<LogicVariable> finalBindings = null;
            State finalState = State.Empty;
            MethodCallFrame finalFrame = predecessor;
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

        private static bool ExactlyOnce(object[] args, TextBuffer output, BindingEnvironment env, Step.Continuation k, MethodCallFrame predecessor)
        {
            ArgumentCountException.Check("ExactlyOnce", 1, args);
            TextBuffer finalOutput = output;
            BindingList<LogicVariable> finalBindings = null;
            State finalState = State.Empty;
            MethodCallFrame finalFrame = predecessor;
            bool failure = true;

            var chain = Step.ChainFromBody("ExactlyOnce", args);
            chain.Try(output, env,
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
                var failedCall = (Call)chain;
                throw new CallFailedException(env.Resolve(failedCall.Task), env.ResolveList(failedCall.Arglist));
            }
            return k(finalOutput, finalBindings, finalState, finalFrame);
        }

        private static bool Max(object[] args, TextBuffer o, BindingEnvironment e, Step.Continuation k, MethodCallFrame predecessor)
        {
            return MaxMinDriver("Max", args, 1, o, e, k, predecessor);
        }

        private static bool Min(object[] args, TextBuffer o, BindingEnvironment e, Step.Continuation k, MethodCallFrame predecessor)
        {
            return MaxMinDriver("Min", args, -1, o, e, k, predecessor);
        }

        /// <summary>
        /// Core implementation of both Max and Min
        /// </summary>
        private static bool MaxMinDriver(string taskName, object[] args,
            int multiplier, TextBuffer o, BindingEnvironment e,
            Step.Continuation k,
            MethodCallFrame predecessor)
        {
            var scoreVar = args[0] as LogicVariable;
            if (scoreVar == null)
                throw new ArgumentInstantiationException(taskName, e, args);

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
                            throw new ArgumentInstantiationException(taskName, new BindingEnvironment(e, u, d), args);

                        default:
                            throw new ArgumentTypeException(taskName, typeof(float), maybeScore, args);
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
                   && k(o.Append(bestResult.Output), bestResult.Bindings, bestResult.State, bestFrame);
        }

        private static bool SaveText(object[] args, TextBuffer o, BindingEnvironment e, Step.Continuation k, MethodCallFrame predecessor)
        {
            ArgumentCountException.Check("SaveText", 2, args);

            var textVar = e.Resolve(args[1]);
            if (textVar == null)
                throw new ArgumentInstantiationException("SaveText", e, args);

            var invocation = args[0] as object[];
            if (invocation == null || invocation.Length == 0)
                throw new ArgumentTypeException("SaveText", typeof(Call), args[0], args);
            var arglist = new object[invocation.Length - 1];
            Array.Copy(invocation, 1, arglist, 0, arglist.Length);
            var call = new Call(invocation[0], arglist, null);
            var initialLength = o.Length;
            string[] chunk = null;
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

        private static bool Parse(object[] args, TextBuffer output, BindingEnvironment env,
            Step.Continuation k, MethodCallFrame predecessor)
        {
            ArgumentCountException.CheckAtLeast(nameof(Parse), 2, args);
            var call = ArgumentTypeException.Cast<object[]>(nameof(Parse),env.Resolve(args[0]), args);
            var text = ArgumentTypeException.Cast<string[]>(nameof(Parse), env.Resolve(args[1]), args);

            var task = call[0] as CompoundTask;
            if (task == null)
                throw new InvalidOperationException(
                    "Task argument to Parse must be a compound task, i.e. a user-defined task with methods.");

            var taskArgs = new object[call.Length - 1];

            var i = 0;
            for (var callIndex = 1; callIndex < call.Length; callIndex++)
                taskArgs[i++] = call[callIndex];

            var parseBuffer = TextBuffer.MakeReadModeTextBuffer(text);
            return task.Call(taskArgs, parseBuffer, env, predecessor,
                (buffer, u, s, p) => buffer.ReadCompleted && k(output, u, s, p));
        }

        #region Data structures for recording execution state
        /// <summary>
        /// Used to record the results of a call so those results can be reapplied later.
        /// Used for all-solutions and maximization meta-predicates
        /// </summary>
        private readonly struct CapturedState
        {
            public readonly string[] Output;
            public readonly BindingList<LogicVariable> Bindings;
            public readonly State State;

            //private static readonly string[] EmptyOutput = new string[0];

            private CapturedState(string[] output, BindingList<LogicVariable> bindings, State state)
            {
                Output = output;
                Bindings = bindings;
                State = state;
            }

            public CapturedState(TextBuffer before, TextBuffer after, BindingList<LogicVariable> bindings,
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
        internal static void GenerateSolutions(string taskName, object[] args, TextBuffer o, BindingEnvironment e, Step.Continuation k, MethodCallFrame predecessor)
        {
            new Call(StateVariableName.Named(taskName), args, null).Try(o, e, k, predecessor);
        }

        /// <summary>
        /// Find all solutions to the specified task and arguments.  Return a list of the arglists for each solution.
        /// </summary>
        internal static List<object[]> AllSolutions(string taskName, object[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame predecessor)
        {
            var results = new List<object[]>();
            GenerateSolutions(taskName, args, o, e, (output, b, d, p) =>
            {
                results.Add(new BindingEnvironment(e, b, d).ResolveList(args));
                return false;
            },
                predecessor);
            return results;
        }

        /// <summary>
        /// Find all solutions to the specified task and arguments.  Return a list of the text outputs of each solution.
        /// </summary>
        internal static List<string[]> AllSolutionText(string taskName, object[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame predecessor)
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
        internal static void GenerateSolutionsFromBody(string callingTaskName, object[] body, TextBuffer o, BindingEnvironment e,
            Step.Continuation k,
            MethodCallFrame predecessor)
        {
            Step.ChainFromBody(callingTaskName, body).Try(o, e, k, predecessor);
        }

        /// <summary>
        /// Find all solutions to the specified sequence of calls.  Return a list of the text outputs of each solution.
        /// </summary>
        internal static List<string[]> AllSolutionTextFromBody(string callingTaskName, object[] body, TextBuffer o,
            BindingEnvironment e, MethodCallFrame predecessor)
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

        private static bool PreviousCall(object[] args, TextBuffer output, BindingEnvironment env,
            Step.Continuation k, MethodCallFrame predecessor)
        {
            ArgumentCountException.Check(nameof(PreviousCall), 1, args);
            var call = ArgumentTypeException.Cast<object[]>(nameof(PreviousCall),args[0], args);
            foreach (var priorGoal in predecessor.GoalChain)
            {
                if (priorGoal.Method.Task != call[0])
                    // Don't bother making the call expression and trying to unify.
                    continue;

                var e = priorGoal.CallExpression;
                if (call.Length == e.Length 
                    && env.UnifyArrays(call, e, out BindingList<LogicVariable> unifications)
                    && k(output, unifications, env.State, predecessor))
                    return true;
            }
            return false;
        }

        private static bool UniqueCall(object[] args, TextBuffer output, BindingEnvironment env,
            Step.Continuation k, MethodCallFrame predecessor)
        {
            ArgumentCountException.CheckAtLeast(nameof(UniqueCall), 1, args);
            var call = ArgumentTypeException.Cast<object[]>(nameof(PreviousCall), args[0], args);
            var task = call[0] as CompoundTask;
            if (task == null)
                throw new InvalidOperationException(
                    "Task argument to UniqueCall must be a compound task, i.e. a user-defined task with methods.");
            
            var taskArgs = new object[call.Length - 1 + args.Length - 1];

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
                    foreach (var priorGoal in predecessor.GoalChain)
                    {
                        if (priorGoal.Method.Task != task)
                            // Don't bother making the call expression and trying to unify.
                            continue;

                        if (env.Unify(fullCall, priorGoal.CallExpression, u, out BindingList<LogicVariable> _))
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
