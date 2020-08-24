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
        public delegate bool Continuation(PartialOutput o, BindingList<LogicVariable> unifications, State state);

        /// <summary>
        /// Attempt to run this step.
        /// </summary>
        /// <param name="output">Output accumulated so far</param>
        /// <param name="e">Variable binding information to use in this step</param>
        /// <param name="k">Procedure to run if this step and the other steps in its chain are successful</param>
        /// <returns>True if all steps in the chain, and the continuation are all successful.  False means we're backtracking</returns>
        public abstract bool Try(PartialOutput output, BindingEnvironment e, Continuation k);

        /// <summary>
        /// Run any remaining steps in the chain, otherwise run the continuation.
        /// </summary>
        /// <returns>True if all steps in the chain, and the continuation are all successful.  False means we're backtracking</returns>
        protected bool Continue(PartialOutput p, BindingEnvironment e, Continuation k)
        {
            if (Next != null)
                return Next.Try(p, e, k);
            return k == null || k(p, e.Unifications, e.State);
        }
    }
}
