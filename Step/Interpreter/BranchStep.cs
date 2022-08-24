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
    internal class BranchStep : BranchingStep
    {
        public readonly string Name;
        private readonly bool shuffle;

        public BranchStep(string name, Step?[] branches, Step? next, bool shuffle) : base(branches, next)
        {
            Name = name;
            this.shuffle = shuffle;
        }

        private Step?[] EffectiveBranches => shuffle ? Branches.Shuffle() : Branches;

        public override bool Try(TextBuffer output, BindingEnvironment e, Continuation k, MethodCallFrame? predecessor)
        {
            foreach (var branch in EffectiveBranches)
            {
                if (branch == null)  // Empty branch, e.g. [case ?x] Something : Something [else] [end]
                {
                    if (Continue(output, e, k, predecessor)) return true;
                }
                else if (branch.Try(output, e, 
                    (o, u, d, newP)=> 
                        Continue(o, new BindingEnvironment(e, u, d), k, newP)
                    , predecessor))
                    return true;
            }

            return false;
        }

        public override string Source => $"[{Name} ...]";

        public override string ToString() => Name;
    }
}