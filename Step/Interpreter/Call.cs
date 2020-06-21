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

namespace Step.Interpreter
{
    [DebuggerDisplay("Call {" + nameof(Task) + "}")]
    public class Call : Step
    {
        public Call(object task, object[] args, Step next) : base(next)
        {
            Task = task;
            Arglist = args;
        }

        public readonly object Task;
        public readonly object[] Arglist;

        public override bool Try(PartialOutput output, BindingEnvironment env, Continuation k)
        {
            var target = env.Resolve(Task);
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

                case PrimitiveTask.Predicate1 p:
                    ArgumentCountException.Check(p, 1, arglist);
                    return p(arglist[0]) && Continue(output, env, k);

                case PrimitiveTask.Predicate2 p:
                    ArgumentCountException.Check(p, 2, arglist);
                    return p(arglist[0], arglist[1]) && Continue(output, env, k);

                case PrimitiveTask.DeterministicTextGenerator0 g:
                    ArgumentCountException.Check(g, 0, arglist);
                    return Continue(output.Append(g()), env, k);

                case PrimitiveTask.DeterministicTextGenerator1 g:
                    ArgumentCountException.Check(g, 1, arglist);
                    return Continue(output.Append(g(arglist[0])), env, k);

                case PrimitiveTask.DeterministicTextGenerator2 g:
                    ArgumentCountException.Check(g, 2, arglist);
                    return Continue(output.Append(g(arglist[0], arglist[1])), env, k);

                case PrimitiveTask.NondeterministicTextGenerator0 g:
                    ArgumentCountException.Check(g, 0, arglist);
                    foreach (var tokens in g())
                        if (Continue(output.Append(tokens), env, k))
                            return true;
                    return false;

                case PrimitiveTask.NondeterministicTextGenerator1 g:
                    ArgumentCountException.Check(g, 1, arglist);
                    foreach (var tokens in g(arglist[0]))
                        if (Continue(output.Append(tokens), env, k))
                            return true;
                    return false;

                case PrimitiveTask.NondeterministicTextGenerator2 g:
                    ArgumentCountException.Check(g, 2, arglist);
                    foreach (var tokens in g(arglist[0], arglist[1]))
                        if (Continue(output.Append(tokens), env, k))
                            return true;
                    return false;

                case LogicVariable v:
                    throw new ArgumentException($"Attempt to call an unbound variable {v}");

                default:
                    throw new ArgumentException($"Unknown task {target} in call");
            }
        }
    }
}
