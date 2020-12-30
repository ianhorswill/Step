#region Copyright
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

using System.Linq;
using Step.Utilities;

namespace Step.Interpreter
{
    /// <summary>
    /// Internal representation of a method for performing a CompoundTask
    /// </summary>
    public class Method
    {
        /// <summary>
        /// Task for which this is a method
        /// </summary>
        public readonly CompoundTask Task;

        /// <summary>
        /// Terms (variables or values) to unify with the arguments in a call to test whether this method is appropriate
        /// </summary>
        public readonly object[] ArgumentPattern;

        /// <summary>
        /// File from which this method was loaded
        /// </summary>
        public readonly string FilePath;
        
        /// <summary>
        /// Starting line number of this method in FilePath
        /// </summary>
        public readonly int LineNumber;

        /// <summary>
        /// LocalVariables used in this method
        /// </summary>
        public readonly LocalVariableName[] LocalVariableNames;

        /// <summary>
        /// First Step in the linked list of steps constituting this method
        /// </summary>
        public readonly Step StepChain;

        internal Method(CompoundTask task, object[] argumentPattern, LocalVariableName[] localVariableNames, Step stepChain, string filePath, int lineNumber)
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
        /// <param name="pre">Predecessor frame</param>
        /// <returns>True if the method and its continuation succeeded</returns>
        public bool Try(object[] args, PartialOutput output, BindingEnvironment env, MethodCallFrame pre, Step.Continuation k)
        {
            // Make stack frame for locals
            var locals = new LogicVariable[LocalVariableNames.Length];
            for (var i = 0; i < LocalVariableNames.Length; i++)
                locals[i] = new LogicVariable(LocalVariableNames[i]);
            var newFrame = new MethodCallFrame(this, env.Unifications, locals, env.Frame, pre);
            MethodCallFrame.CurrentFrame = newFrame;
            var newEnv = new BindingEnvironment(env, newFrame);
            if (newEnv.UnifyArrays(args, ArgumentPattern, out BindingEnvironment finalEnv))
            {
                env.Module.TraceMethod(Module.MethodTraceEvent.Enter, this, args, output, env);
                newFrame.BindingsAtCallTime = finalEnv.Unifications;
                var traceK = env.Module.Trace == null
                    ? k
                    : (newO, newU, newState, predecessor) =>
                    {
                        MethodCallFrame.CurrentFrame = newFrame;
                        env.Module.TraceMethod(Module.MethodTraceEvent.Succeed, this, args, newO,
                            new BindingEnvironment(env, newU, newState));
                        return k(newO, newU, newState, predecessor);
                    };
                if (StepChain?.Try(output, finalEnv, traceK, newFrame) ?? traceK(output, finalEnv.Unifications, finalEnv.State, newFrame))
                    return true;
            }

            MethodCallFrame.CurrentFrame = newFrame;
            env.Module.TraceMethod(Module.MethodTraceEvent.Fail, this, args, output, env);
            return false;
        }

        /// <inheritdoc />
        public override string ToString() => $"Method {HeadString}";

        /// <summary>
        /// The argument pattern for this method expressed as the course code for a call
        /// </summary>
        public string HeadString => Writer.TermToString(ArgumentPattern.Prepend(Task.Name).ToArray());
    }
}