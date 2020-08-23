#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BranchStep.cs" company="Ian Horswill">
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

using Step.Utilities;

namespace Step.Interpreter
{
    internal class BranchStep : Step
    {
        public readonly string Name;
        private readonly Step[] branches;
        private readonly bool shuffle;

        public BranchStep(string name, Step[] branches, Step next, bool shuffle) : base(next)
        {
            Name = name;
            this.branches = branches;
            this.shuffle = shuffle;
        }

        private Step[] Branches => shuffle ? branches.Shuffle() : branches;

        public override bool Try(PartialOutput output, BindingEnvironment e, Continuation k)
        {
            foreach (var branch in Branches)
            {
                if (branch == null)  // Empty branch, e.g. [case ?x] Something : Something [else] [end]
                {
                    if (Continue(output, e, k)) return true;
                }
                else if (branch.Try(output, e, 
                    (o, u, d)=> 
                        Continue(o, new BindingEnvironment(e, u, d), k)))
                    return true;
            }

            return false;
        }

        public override string ToString() => Name;
    }
}