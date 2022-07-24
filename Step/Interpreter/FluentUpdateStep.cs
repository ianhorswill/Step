using System.Collections.Generic;
using System.Linq;
using Step.Parser;

namespace Step.Interpreter
{
    /// <summary>
    /// A step that updates a fluent
    /// </summary>
    internal class FluentUpdateStep : Step
    {
        /// <summary>
        /// The updates made by this step
        /// </summary>
        public readonly (CompoundTask task, object[] args, bool polarity)[] Updates;

        private FluentUpdateStep((CompoundTask task, object[] args, bool polarity)[] updates) : base(null)
        {
            Updates = updates;
        }

        public static void FromExpression(ChainBuilder chain, object[] expression, Module module, string sourceFile, int lineNumber)
        {
            var updates = new List<(CompoundTask task, object[] args, bool polarity)>();
            foreach (var fluent in expression.Skip(1))
            {
                void ThrowInvalidFluentSyntax() => throw new SyntaxError($"Invalid fluent expression: {fluent}", sourceFile, lineNumber);
                var polarity = true;
                if (!(fluent is object[] fluentExp) || fluentExp.Length == 0)
                    ThrowInvalidFluentSyntax();
                if (fluentExp[0].Equals("Not"))
                {
                    polarity = false;
                    if (fluentExp.Length != 2) ThrowInvalidFluentSyntax();
                    fluentExp = fluentExp[1] as object[];
                    if (fluentExp == null) ThrowInvalidFluentSyntax();
                }
                // ReSharper disable once PossibleNullReferenceException
                if (!(fluentExp[0] is string fluentName) || !DefinitionStream.IsGlobalVariableName(fluentName))
                    ThrowInvalidFluentSyntax();

                // ReSharper disable once RedundantArgumentDefaultValue
                var task = module.FindTask(StateVariableName.Named(fluentName), fluentExp.Length - 1, true);
                task.Flags |= CompoundTask.TaskFlags.ReadCache | CompoundTask.TaskFlags.Fallible | CompoundTask.TaskFlags.MultipleSolutions;
                updates.Add((task, chain.CanonicalizeArglist(fluentExp.Skip(1).ToArray()), polarity));
            }

            chain.AddStep(new FluentUpdateStep(updates.ToArray()));
        }

        public override bool Try(TextBuffer output, BindingEnvironment e, Continuation k, MethodCallFrame predecessor)
        {
            var state = e.State;
            foreach (var (task, args, polarity) in Updates) 
                state = task.SetFluent(state, e.ResolveList(args), polarity);
            return Continue(output, new BindingEnvironment(e, e.Unifications, state), k, predecessor);
        }

        public override string Source => "[now ...]";
    }
}
