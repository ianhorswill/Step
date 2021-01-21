#region Copyright
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
using System.Diagnostics;
using System.Linq;
using Step.Utilities;

namespace Step.Interpreter
{
    /// <summary>
    /// Task implemented as a set of methods, each composed of a series of Steps (sub-tasks)
    /// Tasks defined by user code are CompoundTasks
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class CompoundTask
    {
        /// <summary>
        /// Name, for debugging purposes
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// Number of arguments expected by the task
        /// </summary>
        public readonly int ArgCount;
        /// <summary>
        /// Methods for accomplishing the task
        /// </summary>
        public readonly List<Method> Methods = new List<Method>();

        // ReSharper disable once InconsistentNaming
        private DictionaryStateElement<IStructuralEquatable, CachedResult> _cache;

        private DictionaryStateElement<IStructuralEquatable, CachedResult> Cache
        {
            get
            {
                if (_cache != null)
                    return _cache;
                return _cache = new DictionaryStateElement<IStructuralEquatable, CachedResult>(Name + " cache");
            }
        }

        private State StoreResult(State oldState, object[] arglist, CachedResult result)
        {
            if ((Flags & TaskFlags.ReadCache) == 0)
                throw new InvalidOperationException("Attempt to store result to a task without caching enabled.");
         
            return Cache.Add(oldState, arglist, result);
        }

        internal IList<Method> EffectiveMethods => Shuffle ? (IList<Method>)Methods.WeightedShuffle(m => m.Weight) : Methods;

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
            Suffix = 64
        }

        private readonly struct CachedResult
        {
            public readonly bool Success;
            public readonly string[] Text;

            public CachedResult(bool success, string[] text)
            {
                Success = success;
                Text = text;
            }
        }

        internal TaskFlags Flags;

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

        internal CompoundTask(string name, int argCount)
        {
            Name = name;
            ArgCount = argCount;
        }

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
        internal void AddMethod(float weight, object[] argumentPattern, LocalVariableName[] localVariableNames, Step stepChain, TaskFlags newFlags,
            string path, int lineNumber)
        {
            Flags |= newFlags;
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
        public bool Call(object[] arglist, TextBuffer output, BindingEnvironment env, MethodCallFrame predecessor, Step.Continuation k)
        {
            string lastToken = null;
            if (Suffix)
            {
                (lastToken, output) = output.Unappend();
                arglist = new object[] {lastToken};
            }
            
            ArgumentCountException.Check(this, this.ArgCount, arglist);
            var successCount = 0;

            if (ReadCache)
            {
                if (Cache.TryGetValue(env.State, arglist, out var result))
                {
                    if (result.Success)
                    {
                        successCount++;
                        if (k(output.Append(result.Text), env.Unifications, env.State, predecessor))
                            return true;
                    }
                    else 
                        // We have a match on a cached fail result, so force a failure, skipping over the methods.
                        goto failed;
                }
                else
                    foreach (var pair in Cache.Bindings(env.State))
                    {
                        if (pair.Value.Success
                            && env.UnifyArrays((object[]) pair.Key, arglist,
                                out BindingList<LogicVariable> unifications))
                        {
                            successCount++;
                            if (k(output.Append(pair.Value.Text), unifications, env.State, predecessor))
                                return true;
                        }
                    }
            }

            var methods = this.EffectiveMethods;
            for (var index = 0; index < methods.Count && !(this.Deterministic && successCount > 0); index++)
            {
                var method = methods[index];
                if (method.Try(arglist, output, env, predecessor,
                    (o, u, s, newPredecessor) =>
                    {
                        successCount++;
                        if (WriteCache)
                        {
                            var final = env.ResolveList(arglist, u);
                            if (Term.IsGround(final))
                                s = StoreResult(s, final, new CachedResult(true, TextBuffer.Difference(output, o)));
                        }
                        return k(o, u, s, newPredecessor);
                    }))
                    return true;
                if (Suffix)
                    // Undo any overwriting that the method might have done
                    output.Buffer[output.Length - 1] = lastToken;
            }

            failed:
            var currentFrame = MethodCallFrame.CurrentFrame = env.Frame;
            if (currentFrame != null)
                currentFrame.BindingsAtCallTime = env.Unifications;
            if (successCount == 0 && this.MustSucceed)
            {
                throw new CallFailedException(this, arglist);
            }

            if (currentFrame != null)
            {
                env.Module.TraceMethod(Module.MethodTraceEvent.CallFail, currentFrame.Method, currentFrame.Arglist, output,
                    env);
                MethodCallFrame.CurrentFrame = currentFrame.Predecessor;
            }

            // Failure
            return false;
        }

        /// <inheritdoc />
        public override string ToString() => Name;

        /// <summary>
        /// Remove all defined methods for this task
        /// </summary>
        public void EraseMethods()
        {
            Methods.Clear();
        }

        /// <summary>
        /// All the tasks called by this task
        /// </summary>
        public IEnumerable<object> Callees => Methods.SelectMany(m => m.Callees).Distinct();

        /// <summary>
        /// All the Call steps of all the methods of this task
        /// </summary>
        public IEnumerable<Call> Calls => Methods.SelectMany(m => m.Calls).Distinct();
    }
}
