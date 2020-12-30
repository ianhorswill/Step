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

using System.Collections.Generic;
using System.Linq;

namespace Step.Interpreter
{
    /// <summary>
    /// Represents a step in a method
    /// </summary>
    public abstract class Step
    {
        /// <summary>
        /// Make a new step
        /// </summary>
        protected Step(Step next)
        {
            Next = next;
        }

        /// <summary>
        /// Next step in the step chain of the method to which this step belongs.
        /// Null, if this is the last step in the chain.
        /// </summary>
        public Step Next;

        /// <summary>
        /// A continuation is a procedure to call when a step has completed successfully.
        /// It takes as arguments the things that might have changed in the process of running the step.
        /// </summary>
        /// <returns>True if everything completed successfully, false if we need to backtrack</returns>
        public delegate bool Continuation(PartialOutput o, BindingList<LogicVariable> unifications, State state, MethodCallFrame predecessor);

        /// <summary>
        /// Attempt to run this step.
        /// </summary>
        /// <param name="output">Output accumulated so far</param>
        /// <param name="e">Variable binding information to use in this step</param>
        /// <param name="k">Procedure to run if this step and the other steps in its chain are successful</param>
        /// <param name="predecessor">Predecessor frame</param>
        /// <returns>True if all steps in the chain, and the continuation are all successful.  False means we're backtracking</returns>
        public abstract bool Try(PartialOutput output, BindingEnvironment e, Continuation k, MethodCallFrame predecessor);

        /// <summary>
        /// Run any remaining steps in the chain, otherwise run the continuation.
        /// </summary>
        /// <returns>True if all steps in the chain, and the continuation are all successful.  False means we're backtracking</returns>
        protected bool Continue(PartialOutput p, BindingEnvironment e, Continuation k, MethodCallFrame predecessor)
        {
            if (Next != null)
                return Next.Try(p, e, k, predecessor);
            return k == null || k(p, e.Unifications, e.State, predecessor);
        }

        /// <summary>
        /// An empty callee list for use in Callees
        /// </summary>
        internal static object[] EmptyCalleeList = new object[0];
        
        /// <summary>
        /// The callees of just this step, if any
        /// </summary>
        public virtual IEnumerable<object> Callees => EmptyCalleeList;

        /// <summary>
        /// All the steps in the chain starting with this step
        /// </summary>
        public IEnumerable<Step> ChainSteps
        {
            get
            {
                for (var step = this; step != null; step = step.Next)
                    yield return step;
            }
        }

        /// <summary>
        /// All the callees of all the calls in this chain
        /// </summary>
        public IEnumerable<object> CalleesOfChain => ChainSteps.SelectMany(s => s.Callees);
    }
}
