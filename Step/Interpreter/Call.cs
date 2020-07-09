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
        public string SourceText
        {
            get { return CallSourceText(Task, Arglist); }
        }

        /// <summary>
        /// Make an approximation to the source text of a call to the specified task.
        /// </summary>
        public static string CallSourceText(object task, object[] arglist)
        {
            var b = new StringBuilder();
            b.Append('[');
            b.Append(task);
            foreach (var a in arglist)
            {
                b.Append(' ');
                b.Append(a);
            }

            b.Append(']');
            return b.ToString();
        }

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
            var target = originalTarget;
            if (PrimitiveTask.SurrogateTable.TryGetValue(target, out var implementation))
                target = implementation;

            var arglist = env.ResolveList(Arglist);

            switch (target)
            {
                case CompoundTask p:
                    ArgumentCountException.Check(p, p.ArgCount, arglist);
                    foreach (var method in p.Methods)
                        if (method.Try(arglist, output, env, (o, u, s) => Continue(o, new BindingEnvironment(env, u, s), k)))
                            return true;
                    // Failure
                    return false;

                case PrimitiveTask.MetaTask m:
                    return m(arglist, output, env, (o, u, s) => Continue(o, new BindingEnvironment(env, u, s), k));

                case PrimitiveTask.Predicate0 p:
                    ArgumentCountException.Check(originalTarget, 0, arglist);
                    return p() && Continue(output, env, k);

                case PrimitiveTask.Predicate1 p:
                    ArgumentCountException.Check(originalTarget, 1, arglist);
                    return p(arglist[0]) && Continue(output, env, k);

                case PrimitiveTask.Predicate2 p:
                    ArgumentCountException.Check(originalTarget, 2, arglist);
                    return p(arglist[0], arglist[1]) && Continue(output, env, k);

                case PrimitiveTask.PredicateN p:
                    return p(arglist, env) && Continue(output, env, k);

                case PrimitiveTask.DeterministicTextGenerator0 g:
                    ArgumentCountException.Check(originalTarget, 0, arglist);
                    return Continue(output.Append(g()), env, k);

                case PrimitiveTask.DeterministicTextGenerator1 g:
                    ArgumentCountException.Check(originalTarget, 1, arglist);
                    return Continue(output.Append(g(arglist[0])), env, k);

                case PrimitiveTask.DeterministicTextGenerator2 g:
                    ArgumentCountException.Check(originalTarget, 2, arglist);
                    return Continue(output.Append(g(arglist[0], arglist[1])), env, k);

                case PrimitiveTask.DeterministicTextGeneratorMetaTask g:
                    return Continue(output.Append(g(arglist, output, env)), env, k);

                case PrimitiveTask.NondeterministicTextGenerator0 g:
                    ArgumentCountException.Check(originalTarget, 0, arglist);
                    foreach (var tokens in g())
                        if (Continue(output.Append(tokens), env, k))
                            return true;
                    return false;

                case PrimitiveTask.NondeterministicTextGenerator1 g:
                    ArgumentCountException.Check(originalTarget, 1, arglist);
                    foreach (var tokens in g(arglist[0]))
                        if (Continue(output.Append(tokens), env, k))
                            return true;
                    return false;

                case PrimitiveTask.NondeterministicTextGenerator2 g:
                    ArgumentCountException.Check(originalTarget, 2, arglist);
                    foreach (var tokens in g(arglist[0], arglist[1]))
                        if (Continue(output.Append(tokens), env, k))
                            return true;
                    return false;

                case PrimitiveTask.NonDeterministicRelation r:
                    foreach (var bindings in r(arglist, env))
                        if (Continue(output, new BindingEnvironment(env, bindings, env.DynamicState), k))
                            return true;
                    return false;

                case string[] text:
                    return Continue(output.Append(text), env, k);

                case string text:
                    return Continue(output.Append(text), env, k);

                case LogicVariable v:
                    throw new ArgumentException($"Attempt to call an unbound variable {v}");

                default:
                    throw new ArgumentException($"Unknown task {target} in call");
            }
        }
    }
}
