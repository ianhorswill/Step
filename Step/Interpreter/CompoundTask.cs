﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundTask.cs" company="Ian Horswill">
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
using System.Linq;
using Step.Serialization;
using Step.Utilities;

namespace Step.Interpreter
{
    /// <summary>
    /// Task implemented as a set of methods, each composed of a series of Steps (sub-tasks)
    /// Tasks defined by user code are CompoundTasks
    /// </summary>
    public class CompoundTask : Task
    {

        /// <summary>
        /// Number of arguments expected by the task
        /// </summary>
        // ReSharper disable once PossibleInvalidOperationException
        public int ArgCount => ArgumentCount!.Value;

        /// <summary>
        /// Methods for accomplishing the task
        /// </summary>
        public List<Method> Methods = new List<Method>();

        /// <summary>
        /// Dictionary of how many successful or pending calls to this task are on this execution path.
        /// </summary>
        private static readonly DictionaryStateElement<CompoundTask, int> CallCounts =
            new DictionaryStateElement<CompoundTask, int>(nameof(CallCounts));

        /// <summary>
        /// Return the number of successful or pending calls to this task in the specified state.
        /// </summary>
        public int CallCount(State s) => CallCounts.GetValueOrDefault(s, this);

        #region Result cache

        internal class ResultCache : DictionaryStateElement<IStructuralEquatable, CachedResult>, ISerializable
        {
            static ResultCache()
            {
                Deserializer.RegisterHandler(nameof(ResultCache), Deserialize);
            }

            private readonly CompoundTask task;
            public ResultCache(CompoundTask task, IEqualityComparer<IStructuralEquatable> keyComparer, IEqualityComparer<CachedResult> valueComparer)
                : base(task.Name+" cache", keyComparer, valueComparer)
            {
                this.task = task;
            }

            public void Serialize(Serializer s)
            {
                s.Serialize(task);
            }

            private static object Deserialize(Deserializer d)
            {
                var task = d.Expect<CompoundTask>();
                return task.Cache;
            }
        }
        // ReSharper disable once InconsistentNaming
        private ResultCache? _cache;

        private ResultCache Cache
        {
            get
            {
                if (_cache != null)
                    return _cache;
                return _cache = new ResultCache(this, 
                    Function?Term.Comparer.ForFunctions:Term.Comparer.Default,
                    EqualityComparer<CachedResult>.Default);
            }
        }

        private State StoreResult(State oldState, object?[] arglist, CachedResult result)
        {
            if ((Flags & TaskFlags.ReadCache) == 0)
                throw new InvalidOperationException("Attempt to store result to a task without caching enabled.");
         
            return Cache.SetItem(oldState, arglist, result);
        }

        private static readonly string[] EmptyText = new string[0];
        /// <summary>
        /// Update the value of a fluent
        /// </summary>
        /// <param name="oldState">Current global state</param>
        /// <param name="arglist">Argument values for this fluent to update</param>
        /// <param name="truth">New truth value for this fluent on these arguments</param>
        /// <returns>New global state</returns>
        public State SetFluent(State oldState, object?[] arglist, bool truth, TextBuffer output)
        {
            if (!Term.IsGround(arglist))
                throw new ArgumentInstantiationException(this, new BindingEnvironment(), arglist,
                    "The now command can only be used to update ground instances of a fluent.", output);

            return StoreResult(oldState, arglist, new CachedResult(truth, Function?arglist[^1]:null, EmptyText));
        }

        internal readonly struct CachedResult : ISerializable
        {
            public readonly bool Success;
            public readonly object? FunctionValue;
            public readonly string[] Text;

            static CachedResult()
            {
                Deserializer.RegisterHandler(nameof(CachedResult), Deserialize);
            }

            public CachedResult(bool success, object? functionValue, string[] text)
            {
                Success = success;
                FunctionValue = functionValue;
                Text = text;
            }


            public void Serialize(Serializer s)
            {
                s.Serialize(Success);
                s.Space();
                s.Serialize(FunctionValue);
                s.Space();
                s.Serialize(Text);
            }

            private static object Deserialize(Deserializer d)
            {
                var success = (bool)d.Deserialize()!;
                var functionValue = d.Deserialize();
                var text = ((object[])d.Deserialize()!).Cast<string>().ToArray();
                return new CachedResult(success, functionValue, text);
            }

            public override bool Equals(object obj)
            {
                return obj is CachedResult cr
                       && Success == cr.Success
                       && Term.Comparer.Default.Equals(FunctionValue, cr.FunctionValue)
                       && Text.SequenceEqual(cr.Text);
            }

            public bool Equals(CachedResult other)
            {
                return Success == other.Success && Term.Comparer.Default.Equals(FunctionValue, other.FunctionValue) && Text.SequenceEqual(other.Text);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Success, FunctionValue, Text);
            }
        }

        /// <summary>
        /// All the assertions about this predicate stored in this state using [now ...] 
        /// </summary>
        /// <param name="s">State to test</param>
        /// <returns>argument/truth pairs</returns>
        // ReSharper disable once UnusedMember.Global
        public IEnumerable<(object[], bool)> FluentAssertions(State s) 
            => Cache.Bindings(s).Select(b => ((object[])b.Key, b.Value.Success));

        #endregion

        private bool requiresSort;
        internal IList<Method> EffectiveMethods => Shuffle ? (IList<Method>)Methods.WeightedShuffle(m => m.Weight) :
            (requiresSort?SortMethods():Methods);

        private IList<Method> SortMethods()
        {
            // We want a stable sort for this, so we can't use List<T>.Sort.  But OrderByDescending is documented as being stable.
            Methods = new List<Method>(Methods.OrderByDescending(m => m.Weight));
            requiresSort = false;
            return Methods;
        }

        #region Task flags
        /// <summary>
        /// Declared properties of the task
        /// </summary>
        [Flags]
        public enum TaskFlags
        {
            /// <summary>
            /// No properties declared
            /// </summary>
            None = 0,
            /// <summary>
            /// Shuffle methods during execution.  Corresponds to the [randomly] declaration.
            /// </summary>
            Shuffle = 1,
            /// <summary>
            /// Allow this task to retry during backtracking
            /// </summary>
            MultipleSolutions = 2,
            /// <summary>
            /// Don't throw an exception if this task fails
            /// </summary>
            Fallible = 4,
            /// <summary>
            /// This task is called form outside the Step code, so don't show a warning if it isn't called.
            /// </summary>
            Main = 8,
            /// <summary>
            /// Use results stored in the cache
            /// </summary>
            ReadCache = 16,
            /// <summary>
            /// Add results to the cache
            /// </summary>
            WriteCache = 32,
            /// <summary>
            /// This task is a suffix that modifies the last generated token.
            /// </summary>
            Suffix = 64,
            /// <summary>
            /// True if some method has a cool step
            /// </summary>
            ContainsCoolStep = 128,
            /// <summary>
            /// This fluent is also a function, so for ground instances, its last parameter is unique given its other parameters.
            /// </summary>
            Function = 256,
        }

        public TaskFlags Flags;

        /// <summary>
        /// Programmatic interface for declaring attributes of task
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public void Declare(TaskFlags declarationFlags)
        {
            Flags |= declarationFlags;
        }

        /// <summary>
        /// True if the methods of the task should be tried in random order
        /// </summary>
        public bool Shuffle => (Flags & TaskFlags.Shuffle) != 0;

        /// <summary>
        /// True if this task should only ever generate at most one output
        /// </summary>
        public bool Deterministic => (Flags & TaskFlags.MultipleSolutions) == 0;

        /// <summary>
        /// True if it's an error for this call not to succeed at least once
        /// </summary>
        public bool MustSucceed => (Flags & TaskFlags.Fallible) == 0;

        /// <summary>
        /// If true, this task should check its ResultCache for saved results.
        /// </summary>
        public bool ReadCache => (Flags & TaskFlags.ReadCache) != 0;

        /// <summary>
        /// If true, the task should write results back to the cache on successful calls
        /// in which all arguments are instantiated.
        /// </summary>
        public bool WriteCache => (Flags & TaskFlags.WriteCache) != 0;

        /// <summary>
        /// This task replaces the previous token in the output
        /// </summary>
        public bool Suffix => (Flags & TaskFlags.Suffix) != 0;

        /// <summary>
        /// True if some method contains a cool step
        /// </summary>
        public bool ContainsCoolStep => (Flags & TaskFlags.ContainsCoolStep) != 0;

        /// <summary>
        /// This predicate represents a function.  So the final argument of ground
        /// instances is unique given the values of the other arguments.
        /// </summary>
        public bool Function => (Flags & TaskFlags.Function) != 0;
        #endregion

        internal CompoundTask(string name, int argCount) : base(name, argCount)
        { }

        /// <summary>
        /// Add a new method for achieving this task
        /// </summary>
        /// <param name="weight">The relative probability of this method being tried before the other methods</param>
        /// <param name="argumentPattern">Terms (variables or values) to unify with the arguments in a call to test whether this method is appropriate</param>
        /// <param name="localVariableNames">LocalVariables used in this method</param>
        /// <param name="stepChain">Linked list of Step objects to attempt to execute when running this method</param>
        /// <param name="path">File from which the method was read</param>
        /// <param name="lineNumber">Line number where the method starts in the file</param>
        /// <param name="newFlags">Additional flags to set for the task</param>
        internal void AddMethod(float weight, object?[] argumentPattern, LocalVariableName[] localVariableNames, Step? stepChain, TaskFlags newFlags,
            string? path, int lineNumber)
        {
            Flags |= newFlags;
            if (!ContainsCoolStep 
                && stepChain != null
                && stepChain.AnyStep(s => s is CoolStep))
                Flags |= TaskFlags.ContainsCoolStep;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (weight != 1)
                requiresSort = true;
            
            Methods.Add(new Method(this, weight, argumentPattern, localVariableNames, stepChain, path, lineNumber));
        }

        /// <summary>
        /// Call this task with the specified arguments
        /// </summary>
        /// <param name="arglist">Task arguments</param>
        /// <param name="output">Output accumulated so far</param>
        /// <param name="env">Binding environment</param>
        /// <param name="predecessor">Most recently succeeded MethodCallFrame</param>
        /// <param name="k">Continuation</param>
        /// <returns>True if task succeeded and continuation succeeded</returns>
        /// <exception cref="CallFailedException">If the task fails</exception>
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            string? lastToken = null;
            if (Suffix)
            {
                (lastToken, output) = output.Unappend();
                arglist = new object[] {lastToken};
            }

            arglist = env.ResolveList(arglist);
            
            ArgumentCountException.Check(this, this.ArgCount, arglist, output);
            var successCount = 0;

            if (ReadCache)
            {
                // Check for a hit in the cache.  If we find one, we're done.
                // This will always fail when arglist isn't ground.  The next branch
                // handles the non-ground case.
                if (Cache.TryGetValue(env.State, arglist, out var result))
                {
                    if (result.Success)
                    {
                        if (Function)
                        {
                            // We got a hit, and since this is a function, it's the only allowable hit.
                            // So we succeed deterministically.
                            return env.Unify(arglist[^1], result.FunctionValue,
                                       out BindingList? u) &&
                                   k(output.Append(result.Text), u, env.State, predecessor);
                        }
                        else
                        {
                            successCount++;
                            if (k(output.Append(result.Text), env.Unifications, env.State, predecessor))
                                return true;
                        }
                    }
                    else 
                        // We have a match on a cached fail result, so force a failure, skipping over the methods.
                        goto failed;
                }
                // If there are variables in the arglist, exhaustively try all the cache entries
                else if (!Term.IsGround(arglist))
                    foreach (var pair in Shuffle ? Cache.Bindings(env.State) : Cache.Bindings(env.State).ToArray().ShuffleInPlace())
                    {
                        if (pair.Value.Success
                            && env.UnifyArrays((object[]) pair.Key, arglist,
                                out BindingList? unifications))
                        {
                            successCount++;
                            if (k(output.Append(pair.Value.Text), unifications, env.State, predecessor))
                                return true;
                        }
                    }
            }

            if (ContainsCoolStep)
            {
                var s = env.State;
                s = CallCounts.SetItem(s, this, CallCount(s) + 1);
                env = new BindingEnvironment(env, env.Unifications, s);
            }

            var methods = EffectiveMethods;
            for (var index = 0; index < methods.Count && !(this.Deterministic && successCount > 0); index++)
            {
                var method = methods[index];
                if (method.Try(arglist, output, env, predecessor,
                    (o, u, s, newPredecessor) =>
                    {
                        var resultArgs = env.ResolveList(arglist, u, false);
                        if (ReadCache)
                        {
                            if (Cache.TryGetValue(env.State, resultArgs, out var result))
                            {
                                // Already in the cache so we already would have generated this answer
                                return false;
                                //if (result.Success)

                                //{
                                //    if (Function && !Term.LiterallyEqual(resultArgs[^1], result.FunctionValue))
                                //        // It's in the cache as a function with a different value
                                //        return false;
                                //}
                                //else
                                //{
                                //    // It's in the cache as a failure; override it.
                                //    return false;
                                //}
                            }
                        }

                        successCount++;
                        if (WriteCache)
                        {
                            var final = env.ResolveList(arglist, u, false);
                            if (Term.IsGround(final))
                                s = StoreResult(s, final, new CachedResult(true,
                                    Function?final[^1]:null,
                                    TextBuffer.Difference(output, o)));
                        }
                        return k(o, u, s, newPredecessor);
                    }))
                    return true;
                if (Suffix)
                    // Undo any overwriting that the method might have done
                    output.Buffer[output.Length - 1] = lastToken!;
            }

            failed:
            var currentFrame = MethodCallFrame.CurrentFrame = env.Frame;
            if (currentFrame != null)
                currentFrame.BindingsAtCallTime = env.Unifications;
            if (successCount == 0 && this.MustSucceed)
            {
                StepThread.ErrorText(output);
                throw new CallFailedException(this, arglist, output);
            }

            if (currentFrame != null)
            {
                env.Module.TraceMethod(Module.MethodTraceEvent.CallFail, currentFrame.Method!, currentFrame.Arglist, output,
                    env);
                MethodCallFrame.CurrentFrame = currentFrame.Predecessor;
            }

            // Failure
            return false;
        }

        /// <summary>
        /// Remove all defined methods for this task
        /// </summary>
        public void EraseMethods()
        {
            Methods.Clear();
            Flags = TaskFlags.None;
        }

        /// <summary>
        /// Return a state in which the cache has been flushed
        /// </summary>
        /// <param name="s">Old state</param>
        /// <returns>New state in which the cache is empty.</returns>
        public State ClearCache(State s) => Cache.Clear(s);

        /// <summary>
        /// All the tasks called by this task
        /// </summary>
        public IEnumerable<object> Callees => Methods.SelectMany(m => m.Callees).Distinct();

        /// <summary>
        /// All the Call steps of all the methods of this task
        /// </summary>
        public IEnumerable<Call> Calls => Methods.SelectMany(m => m.Calls).Distinct();

        /// <summary>
        /// All the fluent updates of the methods of this task.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public IEnumerable<(CompoundTask task, object?[] args, bool polarity)> FluentUpdates()
        {
            foreach (var m in Methods)
                foreach (var s in Step.ChainSteps(m.StepChain))
                    if (s is FluentUpdateStep u)
                        foreach (var f in u.Updates)
                            yield return f;
        }
    }
}
