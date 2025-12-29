#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Call.cs" company="Ian Horswill">
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
using System.Text;
using Step.Binding;
using Step.Exceptions;
using Step.Output;
using Step.Tasks;
using Step.Terms;

namespace Step.Interpreter.Steps
{
    /// <summary>
    /// A step that involves calling another task as a subtask
    /// </summary>
    [DebuggerDisplay("Call {" + nameof(SourceText) + "}")]
    public class Call : Step
    {
        /// <summary>
        /// A step that involves calling a sub-task
        /// </summary>
        /// <param name="task">A term whose value is the sub-task to execute</param>
        /// <param name="args">Terms for the arguments of the call</param>
        /// <param name="next">Next step in the step chain of whatever method this belongs to</param>
        public Call(object task, object?[] args, Step? next) : base(next)
        {
            Task = task;
            Arglist = args;
        }

        /// <summary>
        /// Make a new call step to the specified task, with no arguments.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public static Call MakeCall(object task, Step? next) => new Call(task, Array.Empty<object?>(), next);

        /// <summary>
        /// Make a new call step to the specified task, with the specified argument.
        /// </summary>
        public static Call MakeCall(object task, object arg1, Step? next) => new Call(task, new[] { arg1 }, next);
        
        /// <summary>
        /// Make a new call step to the specified task, with the specified arguments.
        /// </summary>
        public static Call MakeCall(object task, object arg1, object arg2, Step? next) => new Call(task, new[] { arg1, arg2 }, next);

        /// <summary>
        /// Make a new call step to the specified task, with the specified arguments.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public static Call MakeCall(object task, object arg1, object arg2, object arg3, Step next) => new Call(task, new[] { arg1, arg2, arg3 }, next);

        /// <summary>
        /// Term (e.g. variable) representing the task to call
        /// </summary>
        public readonly object Task;
        /// <summary>
        /// Terms representing the arguments to the subtask.
        /// </summary>
        public readonly object?[] Arglist;

        /// <summary>
        /// Regenerates an approximation to the source code for this call
        /// </summary>
        public string SourceText => CallSourceText(Task, Arglist, Module.RichTextStackTraces);

        internal static readonly StateVariableName MentionHook = StateVariableName.Named("Mention");

        /// <inheritdoc />
        public override IEnumerable<object> Callees => TaskReferencesWithin(Arglist).Prepend(Task);

        public static IEnumerable<object> TaskReferencesWithin(object? o)
        {
            switch (o)
            {
                case StateVariableName _:
                    yield return o;
                    break;

                case Task t:
                    yield return t;
                    break;

                case object[] tuple:
                    foreach (var e in tuple.SelectMany(TaskReferencesWithin))
                        yield return e;
                    break;

                case FeatureStructure s:
                    foreach (var f in s.FeatureValues(null))
                        foreach (var c in TaskReferencesWithin(f.Value))
                            yield return c;
                    break;
            }
        }

        internal override IEnumerable<Call> Calls => new[] {this};
        
        /// <summary>
        /// Attempt to run this task
        /// </summary>
        /// <param name="output">Output to which to write text</param>
        /// <param name="env">Variable binding information</param>
        /// <param name="k">Continuation to call at the end of this step's step-chain</param>
        /// <param name="predecessor">Predecessor frame</param>
        /// <returns>True if this steps, the rest of its step-chain, and the continuation all succeed.</returns>
        public override bool Try(TextBuffer output, BindingEnvironment env, Task.Continuation k,
            MethodCallFrame? predecessor)
        {
            MethodCallFrame.CurrentFrame = env.Frame;
            var target = env.Resolve(Task);
            var arglist = env.ResolveList(Arglist);

            return CallTask(output, env, k, target, arglist, target, predecessor);
        }

        private bool CallTask(TextBuffer output, BindingEnvironment env, Task.Continuation k, object? target, object?[] arglist,
            object? originalTarget, MethodCallFrame? predecessor)
        {
            switch (target)
            {
                case "/":
                    return ElNode.ElLookupPrimitive.Call(arglist, output, env, predecessor,
                        (newOutput, u, s, newPredecessor)
                            => Continue(newOutput, new BindingEnvironment(env, u, s), k, newPredecessor));

                case Task p:
                    return p.Call(arglist, output, env, predecessor,
                        (newOutput, u, s, newPredecessor)
                            => Continue(newOutput, new BindingEnvironment(env, u, s), k, newPredecessor));

                case string[] text:
                    return Continue(output.Append(text), env, k, predecessor);

                case IDictionary d:
                    ArgumentCountException.Check(d, 2, arglist, output);
                    var arg0 = arglist[0];
                    var v0 = arg0 as LogicVariable;
                    var arg1 = arglist[1];
                    if (v0 == null)
                    {
                        return d.Contains(arg0!)
                               && env.Unify(arg1, d[arg0!], out BindingEnvironment e2)
                               && Continue(output, e2, k, predecessor);
                    }
                    else
                    {
                        // Arg 0 is out
                        foreach (DictionaryEntry e in d)
                            if (env.Unify(arg0, e.Key, out BindingList? unif1)
                                && env.Unify(arg1, e.Value, unif1, out var unif2)
                                && Continue(output, new BindingEnvironment(env, unif2, env.State), k, predecessor))
                                return true;
                    }
                    return false;

                case IList l:
                    // If it's a list in the operator position, pretend it's a call to member
                    if (arglist.Length != 1)
                        throw new ArgumentCountException("{list member}", 1, arglist, output);

                    if (arglist[0] is LogicVariable l0)
                    {
                        foreach (var e in l)
                            if (Continue(output,
                                new BindingEnvironment(env, BindingList.Bind(env.Unifications, l0, e),
                                    env.State),
                                k,
                                predecessor))
                                return true;
                    }
                    else
                    {
                        if (l.Contains(arglist[0]) && Continue(output, env, k, predecessor))
                            return true;
                    }
                    return false;

                case LogicVariable v:
                    throw new ArgumentException($"Attempt to call an unbound variable {v}");

                case null:
                    throw new ArgumentException($"Null is not a valid task in call {CallSourceText(originalTarget??"null", arglist, false)}");

                case bool b:
                    if (arglist.Length != 0)
                        throw new ArgumentCountException(b, 0, arglist, output);
                    return b && Continue(output, env, k, predecessor);

                default:
                    if (arglist.Length == 0)
                    {
                        var hook = env.Module.FindTask(MentionHook, 1, false);
                        if (hook != null)
                            return MakeCall(hook, target, Next).Try(output, env, k, predecessor);

                        return Continue(output.Append(target.ToString()), env, k, predecessor);
                    }

                    throw new ArgumentException($"Unknown task {target} in call {CallSourceText(originalTarget??"null", arglist, false)}");
            }
        }

        public static string CallSourceText(object task, object?[] arglist, bool markup, BindingList? unifications = null)
        {
            var b = new StringBuilder();
            var w = new Writer(unifications, b);
            b.Append("[");
            if (markup)
                b.Append("<b>");
            b.Append(task);
            if (markup)
                b.Append("</b>");

            foreach (var arg in arglist)
            {
                b.Append(' ');
                w.Write(arg);
            }

            b.Append(']');

            return b.ToString();
        }

        /// <inheritdoc />
        public override string GetSource(bool markup) => CallSourceText(Task, Arglist, markup);
    }
}
