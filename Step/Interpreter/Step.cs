#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Step.cs" company="Ian Horswill">
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
using System.Linq;

namespace Step.Interpreter
{
    /// <summary>
    /// Represents a step in a method
    /// </summary>
    public abstract class Step
    {
        protected Step(Step next)
        {
            Next = next;
        }

        public static Step Sequence(params object[] steps)
        {
            Step next = null;
            for (var i = steps.Length - 1; i >= 0; i--)
            {
                var step = steps[i];
                switch (step)
                {
                    case string[] tokens:
                        next = new EmitStep(tokens, next);
                        break;

                        case object[] call:
                            next = new Call(call[0], call.Skip(1).ToArray(), next);
                            break;

                        default:
                            throw new ArgumentException($"Unknown step argument in Step.Sequence: {step}");
                }
            }

            return next;
        }

        internal string Expand(Module g)
        {
            string result = null;
            Try(PartialOutput.NewEmpty(), new BindingEnvironment(g, new LogicVariable[0]), (o, u, s) =>
            {
                result = o.AsString;
                return true;
            }); 
            return result;
        }

        internal string Expand()
        {
            return Expand(new Module());
        }

        public delegate bool Continuation(PartialOutput o, BindingList<LogicVariable> unifications, BindingList<GlobalVariable> dynamicState);

        public abstract bool Try(PartialOutput output, BindingEnvironment e, Continuation k);

        protected bool Continue(PartialOutput p, BindingEnvironment e, Continuation k)
        {
            if (Next != null)
                return Next.Try(p, e, k);
            return k == null || k(p, e.Unifications, e.DynamicState);
        }

        public Step Next;
    }
}
