﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Method.cs" company="Ian Horswill">
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
    /// Internal representation of a method for performing a CompoundTask
    /// </summary>
    internal class Method
    {
        /// <summary>
        /// Task for which this is a method
        /// </summary>
        public readonly CompoundTask Task;

        /// <summary>
        /// Terms (variables or values) to unify with the arguments in a call to test whether this method is appropriate
        /// </summary>
        public readonly object[] ArgumentPattern;

        public readonly string FilePath;
        public readonly int LineNumber;

        /// <summary>
        /// LocalVariables used in this method
        /// </summary>
        public readonly LocalVariableName[] LocalVariableNames;

        /// <summary>
        /// First Step in the linked list of steps constituting this method
        /// </summary>
        public readonly Step StepChain;

        public Method(CompoundTask task, object[] argumentPattern, LocalVariableName[] localVariableNames, Step stepChain, string filePath, int lineNumber)
        {
            Task = task;
            ArgumentPattern = argumentPattern;
            LocalVariableNames = localVariableNames;
            StepChain = stepChain;
            FilePath = filePath;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// Attempt to run this method
        /// </summary>
        /// <param name="args">Arguments from the call to the method's task</param>
        /// <param name="output">Output buffer to write to</param>
        /// <param name="env">Variable binding information</param>
        /// <param name="k">Continuation to call if method succeeds</param>
        /// <returns>True if the method and its continuation succeeded</returns>
        public bool Try(object[] args, PartialOutput output, BindingEnvironment env, Step.Continuation k)
        {
            // Make stack frame for locals
            var locals = new LogicVariable[LocalVariableNames.Length];
            for (var i = 0; i < LocalVariableNames.Length; i++)
                locals[i] = new LogicVariable(LocalVariableNames[i]);
            var newFrame = new MethodCallFrame(this, env.Unifications, locals, env.Frame);
            MethodCallFrame.CurrentFrame = newFrame;
            var newEnv = new BindingEnvironment(env, newFrame);
            if (newEnv.UnifyArrays(args, ArgumentPattern, out BindingEnvironment finalEnv))
            {
                newFrame.BindingsAtCallTime = finalEnv.Unifications;
                if (StepChain?.Try(output, finalEnv, k) ?? k(output, finalEnv.Unifications, finalEnv.DynamicState))
                    return true;
            }

            return false;
        }

        public override string ToString() => $"Method of {Task.Name}";
    }
}