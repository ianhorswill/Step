using Step.Binding;
using Step.Output;
using Step.Tasks;
using Step.Tasks.Primitives;
using System;
using System.Linq;

namespace Step.Interpreter.Steps
{
    /// <summary>
    /// A step that updates a fluent
    /// </summary>
    internal class FluentUpdateStep : Step
    {
        /// <summary>
        /// The updates made by this step
        /// </summary>
        public readonly object?[] Updates;

        private FluentUpdateStep(object?[] updates) : base(null)
        {
            Updates = updates;
        }

        public static void FromExpression(ChainBuilder chain, object?[] expression, Module module, string? sourceFile, int lineNumber)
        {
            //var updates = new List<(CompoundTask task, object?[] args, bool polarity)>();
            
            chain.AddStep(new FluentUpdateStep(expression.Skip(1).ToArray()));
        }

        public override bool Try(TextBuffer output, BindingEnvironment e, Task.Continuation k, MethodCallFrame? predecessor)
        {
            
            var state = e.State;
            foreach (var fluent in Updates)
            {
                void ThrowInvalidFluentSyntax() => throw new ArgumentException($"Invalid fluent expression: {fluent}");

                var fluentExp = e.Resolve(fluent) as object?[];
                if (fluentExp == null || fluentExp.Length == 0)
                    ThrowInvalidFluentSyntax();
                var polarity = true;
                if (Equals(fluentExp![0], HigherOrderBuiltins.Not))
                {
                    polarity = false;
                    if (fluentExp.Length != 2) ThrowInvalidFluentSyntax();
                    fluentExp = (fluentExp[1] as object?[])!;
                    if (fluentExp.Length > 1 && fluentExp[1] == null) ThrowInvalidFluentSyntax();
                }
                // ReSharper disable once PossibleNullReferenceException
                if (!(fluentExp[0] is CompoundTask task))
                    ThrowInvalidFluentSyntax();

                // ReSharper disable once RedundantArgumentDefaultValue
                task.Flags |= CompoundTask.TaskFlags.ReadCache | CompoundTask.TaskFlags.Fallible | CompoundTask.TaskFlags.MultipleSolutions;
                state = task.SetFluent(state, fluentExp.Skip(1).ToArray(), polarity, output);
            }
            return Continue(output, new BindingEnvironment(e, e.Unifications, state), k, predecessor);
        }

        public override string GetSource(bool markup) => "[now ...]";
    }
}
