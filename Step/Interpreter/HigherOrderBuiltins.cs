#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HigherOrderBuiltins.cs" company="Ian Horswill">
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
using System.Collections.Generic;
using System.Linq;
using static Step.Interpreter.PrimitiveTask;

namespace Step.Interpreter
{
    /// <summary>
    /// Implementations of higher-order builtin primitives.
    /// </summary>
    internal static class HigherOrderBuiltins
    {
        private class NonLocalExit : Exception
        {
            public readonly PartialOutput Output;
            public readonly BindingList<LogicVariable> Environment;
            public readonly BindingList<GlobalVariableName> DynamicState;

            private NonLocalExit(PartialOutput output, BindingList<LogicVariable> environment, BindingList<GlobalVariableName> dynamicState)
            {
                Output = output;
                Environment = environment;
                DynamicState = dynamicState;
            }

            public static bool Throw(PartialOutput output, BindingList<LogicVariable> environment,
                BindingList<GlobalVariableName> dynamicState)
            {
                throw new NonLocalExit(output, environment, dynamicState);
            }
        }

        internal static void DefineGlobals()
        {
            var g = Module.Global;

            g["DoAll"] = (DeterministicTextGeneratorMetaTask) DoAll;
            g["Once"] = (MetaTask) Once;
            g["ExactlyOnce"] = (MetaTask) ExactlyOnce;
        }

        private static IEnumerable<string> DoAll(object[] args, PartialOutput o, BindingEnvironment e)
        {
            return AllSolutionTextFromBody("DoAll", args, o, e).SelectMany(strings => strings);
        }

        private static bool Once(object[] args, PartialOutput o, BindingEnvironment e, Step.Continuation k)
        {
            try
            {
                StepChainFromBody("Once", args).Try(o, e, NonLocalExit.Throw);
            }
            catch (NonLocalExit x)
            {
                return k(x.Output, x.Environment, x.DynamicState);
            }

            return false;
        }

        private static bool ExactlyOnce(object[] args, PartialOutput o, BindingEnvironment e, Step.Continuation k)
        {
            ArgumentCountException.Check("ExactlyOnce", 1, args);
            var chain = StepChainFromBody("Once", args);
            try
            {
                chain.Try(o, e, (output, u, d) => NonLocalExit.Throw(output, u, d));
            }
            catch (NonLocalExit x)
            {
                return k(x.Output, x.Environment, x.DynamicState);
            }

            var failedCall = (Call) chain;
            throw new CallFailedException(failedCall.Task, e.ResolveList(failedCall.Arglist));
        }

        #region Utilities for higher-order primitives
        /// <summary>
        /// Calls a task with the specified arguments and allows the user to provide their own continuation.
        /// The only (?) use case for this is when you want to forcibly generate multiple solutions
        /// </summary>
        internal static void GenerateSolutions(string taskName, object[] args, PartialOutput o, BindingEnvironment e, Step.Continuation k)
        {
            new Call(GlobalVariableName.Named(taskName), args, null).Try(o, e, k);
        }

        /// <summary>
        /// Find all solutions to the specified task and arguments.  Return a list of the arglists for each solution.
        /// </summary>
        internal static List<object[]> AllSolutions(string taskName, object[] args, PartialOutput o, BindingEnvironment e)
        {
            var results = new List<object[]>();
            GenerateSolutions(taskName, args, o, e, (output, b, d) =>
            {
                results.Add(new BindingEnvironment(e, b, d).ResolveList(args));
                return false;
            });
            return results;
        }

        /// <summary>
        /// Find all solutions to the specified task and arguments.  Return a list of the text outputs of each solution.
        /// </summary>
        internal static List<string[]> AllSolutionText(string taskName, object[] args, PartialOutput o, BindingEnvironment e)
        {
            var results = new List<string[]>();
            var initialLength = o.Length;
            GenerateSolutions(taskName, args, o, e, (output, b, d) =>
            {
                var chunk = new string[output.Length - initialLength];
                for (var i = initialLength; i < output.Length; i++)
                    chunk[i - initialLength] = o.Buffer[i];
                results.Add(chunk);
                return false;
            });
            return results;
        }

        /// <summary>
        /// Calls all the tasks in the body and allows the user to provide their own continuation.
        /// The only (?) use case for this is when you want to forcibly generate multiple solutions
        /// </summary>
        internal static void GenerateSolutionsFromBody(string callingTaskName, object[] body, PartialOutput o, BindingEnvironment e, Step.Continuation k)
        {
            StepChainFromBody(callingTaskName, body).Try(o, e, k);
        }

        internal static Step StepChainFromBody(string callingTaskName, object[] body)
        {
            Step chain = null;
            for (var i = body.Length - 1; i >= 0; i--)
            {
                if (body[i].Equals("\n"))
                    continue;
                var invocation = body[i] as object[];
                if (invocation == null  || invocation.Length == 0)
                    throw new ArgumentTypeException(callingTaskName, typeof(Call), body[i]);
                var arglist = new object[invocation.Length - 1];
                Array.Copy(invocation, 1, arglist, 0, arglist.Length);
                chain = new Call(invocation[0], arglist, chain);
            }

            return chain;
        }

        /// <summary>
        /// Find all solutions to the specified sequence of calls.  Return a list of the text outputs of each solution.
        /// </summary>
        internal static List<string[]> AllSolutionTextFromBody(string callingTaskName, object[] body, PartialOutput o, BindingEnvironment e)
        {
            var results = new List<string[]>();
            var initialLength = o.Length;
            GenerateSolutionsFromBody(callingTaskName, body, o, e, (output, b, d) =>
            {
                var chunk = new string[output.Length - initialLength];
                for (var i = initialLength; i < output.Length; i++)
                    chunk[i - initialLength] = o.Buffer[i];
                results.Add(chunk);
                return false;
            });
            return results;
        }
        #endregion
    }
}
