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
using System.Text;

namespace Step.Interpreter
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
        public Call(object task, object[] args, Step next) : base(next)
        {
            Task = task;
            Arglist = args;
        }

        /// <summary>
        /// Make a new call step to the specified task, with no arguments.
        /// </summary>
        public static Call MakeCall(object task, Step next) => new Call(task, new object[0], next);

        /// <summary>
        /// Make a new call step to the specified task, with the specified argument.
        /// </summary>
        public static Call MakeCall(object task, object arg1, Step next) => new Call(task, new[] { arg1 }, next);
        
        /// <summary>
        /// Make a new call step to the specified task, with the specified arguments.
        /// </summary>
        public static Call MakeCall(object task, object arg1, object arg2, Step next) => new Call(task, new[] { arg1, arg2 }, next);

        /// <summary>
        /// Make a new call step to the specified task, with the specified arguments.
        /// </summary>
        public static Call MakeCall(object task, object arg1, object arg2, object arg3, Step next) => new Call(task, new[] { arg1, arg2, arg3 }, next);

        /// <summary>
        /// Term (e.g. variable) representing the task to call
        /// </summary>
        public readonly object Task;
        /// <summary>
        /// Terms representing the arguments to the subtask.
        /// </summary>
        public readonly object[] Arglist;

        /// <summary>
        /// Regenerates an approximation to the source code for this call
        /// </summary>
        public string SourceText => CallSourceText(Task, Arglist);

        internal static readonly GlobalVariableName MentionHook = GlobalVariableName.Named("Mention");

        /// <summary>
        /// Attempt to run this task
        /// </summary>
        /// <param name="output">Output to which to write text</param>
        /// <param name="env">Variable binding information</param>
        /// <param name="k">Continuation to call at the end of this step's step-chain</param>
        /// <returns>True if this steps, the rest of its step-chain, and the continuation all succeed.</returns>
        public override bool Try(PartialOutput output, BindingEnvironment env, Continuation k)
        {
            var originalTarget = env.Resolve(Task);
            var target = PrimitiveTask.GetSurrogate(originalTarget);
            var arglist = env.ResolveList(Arglist);

            return CallTask(output, env, k, target, arglist, originalTarget);
        }

        private bool CallTask(PartialOutput output, BindingEnvironment env, Continuation k, object target, object[] arglist,
            object originalTarget)
        {
            switch (target)
            {
                case CompoundTask p:
                    ArgumentCountException.Check(p, p.ArgCount, arglist);
                    var successCount = 0;
                    var methods = p.EffectiveMethods;
                    for (var index = 0; index < methods.Count && !(p.Deterministic && successCount > 0); index++)
                    {
                        var method = methods[index];
                        if (method.Try(arglist, output, env, (o, u, s) =>
                        {
                            successCount++;
                            return Continue(o, new BindingEnvironment(env, u, s), k);
                        }))
                            return true;
                    }

                    if (successCount == 0 && p.MustSucceed)
                        throw new CallFailedException(p, arglist);

                    // Failure
                    return false;

                case PrimitiveTask.MetaTask m:
                    return m(arglist, output, env, (o, u, s) => Continue(o, new BindingEnvironment(env, u, s), k));

                case PrimitiveTask.Predicate0 p:
                    ArgumentCountException.Check(PrimitiveTask.PrimitiveName(originalTarget), 0, arglist);
                    return p() && Continue(output, env, k);

                case PrimitiveTask.Predicate1 p:
                    ArgumentCountException.Check(PrimitiveTask.PrimitiveName(originalTarget), 1, arglist);
                    return p(arglist[0]) && Continue(output, env, k);

                case PrimitiveTask.Predicate2 p:
                    ArgumentCountException.Check(PrimitiveTask.PrimitiveName(originalTarget), 2, arglist);
                    return p(arglist[0], arglist[1]) && Continue(output, env, k);

                case PrimitiveTask.PredicateN p:
                    return p(arglist, env) && Continue(output, env, k);

                case PrimitiveTask.DeterministicTextGenerator0 g:
                    ArgumentCountException.Check(PrimitiveTask.PrimitiveName(originalTarget), 0, arglist);
                    return Continue(output.Append(g()), env, k);

                case PrimitiveTask.DeterministicTextGenerator1 g:
                    ArgumentCountException.Check(PrimitiveTask.PrimitiveName(originalTarget), 1, arglist);
                    return Continue(output.Append(g(arglist[0])), env, k);

                case PrimitiveTask.DeterministicTextGenerator2 g:
                    ArgumentCountException.Check(PrimitiveTask.PrimitiveName(originalTarget), 2, arglist);
                    return Continue(output.Append(g(arglist[0], arglist[1])), env, k);

                case PrimitiveTask.DeterministicTextGeneratorMetaTask g:
                    return Continue(output.Append(g(arglist, output, env)), env, k);

                case PrimitiveTask.NondeterministicTextGenerator0 g:
                    ArgumentCountException.Check(PrimitiveTask.PrimitiveName(originalTarget), 0, arglist);
                    foreach (var tokens in g())
                        if (Continue(output.Append(tokens), env, k))
                            return true;
                    return false;

                case PrimitiveTask.NondeterministicTextGenerator1 g:
                    ArgumentCountException.Check(PrimitiveTask.PrimitiveName(originalTarget), 1, arglist);
                    foreach (var tokens in g(arglist[0]))
                        if (Continue(output.Append(tokens), env, k))
                            return true;
                    return false;

                case PrimitiveTask.NondeterministicTextGenerator2 g:
                    ArgumentCountException.Check(PrimitiveTask.PrimitiveName(originalTarget), 2, arglist);
                    foreach (var tokens in g(arglist[0], arglist[1]))
                        if (Continue(output.Append(tokens), env, k))
                            return true;
                    return false;

                case PrimitiveTask.NonDeterministicRelation r:
                    foreach (var bindings in r(arglist, env))
                        if (Continue(output, new BindingEnvironment(env, bindings, env.DynamicState), k))
                            return true;
                    return false;

                case Cons l:
                    // If it's a list in the operator position, pretend it's a call to member
                    if (arglist.Length != 1)
                        throw new ArgumentCountException("<list member>", 1, arglist);
                    var member = (PrimitiveTask.NonDeterministicRelation) env.Module["Member"];
                    foreach (var bindings in member(new[] { arglist[0], l }, env))
                        if (Continue(output, new BindingEnvironment(env, bindings, env.DynamicState), k))
                            return true;
                    return false;

                case LogicVariable v:
                    throw new ArgumentException($"Attempt to call an unbound variable {v}");

                case null:
                    throw new ArgumentException($"Null is not a valid task in call {CallSourceText(originalTarget, arglist)}");

                default:
                    if (arglist.Length == 0)
                    {
                        var hook = env.Module.FindTask(MentionHook, 1, false);
                        if (hook != null)
                            return MakeCall(hook, target, Next).Try(output, env, k);
                        if (target is string[] text)
                            return Continue(output.Append(text), env, k);
                        return Continue(output.Append(target.ToString()), env, k);
                    }

                    throw new ArgumentException($"Unknown task {target} in call {CallSourceText(originalTarget, arglist)}");
            }
        }

        internal static string CallSourceText(object task, object[] arglist)
        {
            var b = new StringBuilder();
            b.Append('[');
            b.Append(task);
            foreach (var arg in arglist)
            {
                b.Append(' ');
                var a = PrimitiveTask.PrimitiveName(arg);
                if (a == null)
                    b.Append("null");
                else if (a is string s)
                {
                    b.Append("\"");
                    b.Append(s);
                    b.Append("\"");
                }
                else
                {
                    var asString = a.ToString();
                    if (asString.IndexOf(' ') < 0)
                        b.Append(a);
                    else
                        b.Append($"<{asString}>");
                }
            }

            b.Append(']');

            return b.ToString();
        }
    }
}
