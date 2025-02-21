﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Step.Output;

namespace Step.Interpreter
{
    /// <summary>
    /// Reifies a call to a method
    /// Used only so that there's a data structure that can be walked to generate a stack backtrace
    /// NOT THREAD SAFE
    /// </summary>
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class MethodCallFrame
    {
        /// <summary>
        /// Maximum number of method calls in a successful execution path.
        /// </summary>
        public static int MaxStackDepth = 500;
        
        /// <summary>
        /// The MethodCallFrame for the most recently called frame
        /// NOT THREAD SAFE
        /// </summary>
        public static MethodCallFrame? CurrentFrame { get; internal set; }

        /// <summary>
        /// The method being called
        /// </summary>
        public readonly Method? Method;

        public string MethodSource => Method != null?Method.MethodCode:"No source";

        public string MethodSourceNoMarkup => Method != null?Method.GetSource(false):"No source";

        /// <summary>
        /// The task being called in this frame.
        /// </summary>
        public CompoundTask Task => Method!.Task;
        
        /// <summary>
        /// The logic variable binding list at the time of the call
        /// </summary>
        public BindingList? BindingsAtCallTime { get; internal set; }
        
        /// <summary>
        /// The local variables of the environment of the call
        /// </summary>
        public readonly LogicVariable[] Locals;
        
        /// <summary>
        /// Caller's frame - this is the frame of the calling method, not the most recently executed task
        /// The two are the same for deterministic languages, but can be different for non-deterministic ones
        /// For example, if A calls B then C and B calls D, then on entry to D, then the method call frame chain
        /// entry to C is just C -> A.
        /// 
        /// However, the real C# execution stack looks like:
        ///    C -> D -> B -> A
        /// Because if C fails, we have to backtrack to D, not to A.  
        /// </summary>
        public readonly MethodCallFrame? Caller;

        /// <summary>
        /// The method that succeeded immediately before this call
        /// </summary>
        public readonly MethodCallFrame? Predecessor;

        /// <summary>
        /// The chain of this frame and its callers
        /// </summary>
        public IEnumerable<MethodCallFrame> CallerChain
        {
            get
            {
                for (var frame = this; frame != null; frame = frame.Caller)
                    yield return frame;
            }
        }

        /// <summary>
        /// The chain of this frame and its predecessors
        /// </summary>
        /// <param name="methodCallFrame"></param>
        public static IEnumerable<MethodCallFrame> GoalChain(MethodCallFrame? methodCallFrame)
        {
            for (var frame = methodCallFrame; frame != null; frame = frame.Predecessor)
                yield return frame;
        }

        /// <summary>
        /// Number of calls deep this call appears on the stack.
        /// </summary>
        public readonly uint StackDepth;

        internal MethodCallFrame(Method? method, BindingList? bindings, LogicVariable[] locals, MethodCallFrame? caller, MethodCallFrame? predecessor)
        {
            Method = method;
            BindingsAtCallTime = bindings;
            Locals = locals;
            Caller = caller;
            Predecessor = predecessor;
            StackDepth = predecessor?.StackDepth + 1 ?? 0;
            if (StackDepth > MaxStackDepth)
                throw new StackOverflowException("Maximum interpreter stack depth in Step program");
        }

        /// <summary>
        /// The effective argument list of the call
        /// This has to get reconstructed from the ArgumentPattern of the method,
        /// which is fixed across all calls and contains LocalVariableName objects
        /// in place of the actual LogicVariables they name (since the latter vary
        /// from call to call), and the Locals array, which contains the specific
        /// logicVariables used in this particular call.
        /// </summary>
        public object?[] Arglist
        {
            get
            {
                object? Resolve(object? o)
                {
                    switch (o)
                    {
                        case LocalVariableName n:
                            return BindingEnvironment.Deref(Locals[n.Index], BindingsAtCallTime);

                        case LogicVariable l:
                            return BindingEnvironment.Deref(l, BindingsAtCallTime);

                        case object?[] tuple:
                            return tuple.Select(Resolve).ToArray();

                        default:
                            return o;
                    }
                }

                if (Method == null)
                    return Array.Empty<object?>();
                
                return Method.ArgumentPattern.Select(Resolve).ToArray();
            }
        }
        
        /// <summary>
        /// Regenerates the textual version of the call in this frame
        /// </summary>
        /// <param name="unifications">Binding list currently in effect.  This will generally be whatever the most recent binding list of the interpreter is.</param>
        public string GetCallSourceText(bool markup, BindingList? unifications)
        {
            var source = Call.CallSourceText(Method!.Task, Arglist, markup, unifications);
            if (Method.FilePath == null)
                return source;
            var start = markup ? "\n     <i>" : "(";
            var end = markup ? "</i>" : ")";
            return $"{source} {start}at {Path.GetFileName(Method.FilePath)}:{Method.LineNumber}{end}";
        }

        public string CallSourceText => GetCallSourceText(Module.RichTextStackTraces, BindingsAtCallTime);

        public string CallSourceTextWithoutFileName => Call.CallSourceText(Method!.Task, Arglist, Module.RichTextStackTraces, BindingsAtCallTime);

        public string CallSourceTextWithCurrentBindings =>
            Call.CallSourceText(Method!.Task, Arglist, Module.RichTextStackTraces, MostRecentBindings);

        public static BindingList? CurrentFrameBindings => CurrentFrame?.BindingsAtCallTime;
        public static BindingList? MostRecentBindings => StepThread.Current == null
            ? CurrentFrameBindings
            : StepThread.Current.Environment == null
                ? CurrentFrameBindings
                : StepThread.Current.Environment.Value.Unifications;

        public string Summary
        {
            get
            {
                var b = new StringBuilder();
                b.Append(CallSourceTextWithCurrentBindings);
                if (Method != null)
                {
                    b.Append("\n");
                    b.Append(Method.MethodCode);
                    foreach (var v in LocalVariableValuesForDisplay)
                        b.Append($"\n{v.Variable} = {v.Value}");
                }
                return b.ToString();
            }
        }


        public class BindingForDisplay
        {
            public string Variable { get; private set; }
            public string Value { get; private set; }

            public BindingForDisplay(LogicVariable v, BindingList? b)
            {
                Variable = v.ToString();
                var d = BindingEnvironment.Deref(v, b);
                Value = v==d?"unbound":Writer.TermToString(d, b);
            }
        }

        public IEnumerable<BindingForDisplay> LocalVariableValuesForDisplay =>
            Locals.Select(l => new BindingForDisplay(l, MostRecentBindings));

        private object?[]? cachedCallExpression;
        
        /// <summary>
        /// Regenerate a tuple representing this call.
        /// </summary>
        public object?[] CallExpression
        {
            get
            {
                if (cachedCallExpression != null)
                    return cachedCallExpression;
                
                var result = new object?[Arglist.Length + 1];
                result[0] = Method!.Task;
                for (var i = 0; i < Arglist.Length; i++)
                    result[i + 1] = Arglist[i];

                cachedCallExpression = result;
                return result;
            }
        }

        private string DebuggerDisplay => ToString();

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Call to {Method}";
        }
    }
}
