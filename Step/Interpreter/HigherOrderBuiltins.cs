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
        internal static void DefineGlobals()
        {
            var g = Module.Global;

            g["DoAll"] =
                (DeterministicTextGeneratorMetaTask) ((args, o, e) =>
                    AllSolutionTextFromBody("DoAll", args, o, e).SelectMany(strings => strings));
            g["Once"] =
                (MetaTask) ((args, o, e, k) => StepChainFromBody("Once", args).Try(o, e, k));
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
