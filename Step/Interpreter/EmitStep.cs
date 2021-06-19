#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EmitStep.cs" company="Ian Horswill">
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
    /// A step that dumps a fixed set of tokens to the output.
    /// EmitSteps always succeed
    /// </summary>
    internal class EmitStep : Step
    {
        public EmitStep(string[] text, Step next) : base(next)
        {
            Text = text;
        }

        /// <summary>
        /// Fixed sequence of tokens to output when this step is performed
        /// </summary>
        public readonly string[] Text;

        /// <summary>
        /// Output Text and succeed.
        /// </summary>
        /// <param name="output">When to write the text</param>
        /// <param name="e">Variable info.  Not used, but passed on to continuation</param>
        /// <param name="k">Continuation to run after the end of this method.</param>
        /// <param name="predecessor">Predecessor frame</param>
        /// <returns></returns>
        public override bool Try(TextBuffer output, BindingEnvironment e, Continuation k, MethodCallFrame predecessor) => 
            output.Unify(Text, out var result) && Continue(result, e, k, predecessor);

        public override string Source => string.Join(" ", Text);
    }
}
