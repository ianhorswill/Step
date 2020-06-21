#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundTask.cs" company="Ian Horswill">
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
using System.Diagnostics;

namespace Step.Interpreter
{
    /// <summary>
    /// Task implemented as a set of methods, each composed of a series of Steps (sub-tasks)
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class CompoundTask
    {
        public readonly string Name;
        public readonly int ArgCount;
        internal readonly List<Method> Methods = new List<Method>();

        public CompoundTask(string name, int argCount)
        {
            Name = name;
            ArgCount = argCount;
        }

        public void AddMethod(object[] argumentPattern, LocalVariableName[] localVariableNames, Step stepChain) 
            => Methods.Add(new Method(this, argumentPattern, localVariableNames, stepChain));

        public string Call(Module m, params object[] arglist)
        {
            var output = PartialOutput.NewEmpty();
            var env = new BindingEnvironment(m, null);
            string result = null;
            foreach (var method in Methods)
                if (method.Try(arglist, output, env, (o, u, s) => { result = o.AsString; return true; }))
                    return result;
            return null;
        }

        internal class Method
        {
            /// <summary>
            /// Task for which this is a method
            /// </summary>
            public readonly CompoundTask Task;

            public readonly object[] ArgumentPattern;

            /// <summary>
            /// Number of local variables 
            /// </summary>
            public readonly LocalVariableName[] LocalVariableNames;

            /// <summary>
            /// First Step in the linked list of steps constituting this task
            /// </summary>
            public readonly Step StepChain;

            public Method(CompoundTask task, object[] argumentPattern, LocalVariableName[] localVariableNames, Step stepChain)
            {
                Task = task;
                ArgumentPattern = argumentPattern;
                LocalVariableNames = localVariableNames;
                StepChain = stepChain;
            }

            public bool Try(object[] args, PartialOutput output, BindingEnvironment env, Step.Continuation k)
            {
                // Make stack frame for locals
                var locals = new LogicVariable[LocalVariableNames.Length];
                for (var i = 0; i < LocalVariableNames.Length; i++)
                    locals[i] = new LogicVariable(LocalVariableNames[i]);
                var newEnv = new BindingEnvironment(env, locals);
                return newEnv.UnifyArrays(args, ArgumentPattern, out BindingEnvironment finalEnv)
                       && (StepChain?.Try(output, finalEnv, k) ?? k(output, finalEnv.Unifications, finalEnv.DynamicState));
            }

            public override string ToString() => $"Method of {Task.Name}";
        }
    }
}
