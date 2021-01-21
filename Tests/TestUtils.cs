using System;
using System.Linq;
using Step;
using Step.Interpreter;

namespace Tests
{
    public static class TestUtils
    {
        public static Module Module(params string[] definitions)
        {
            var m = new Module("test");
            m.AddDefinitions(definitions);
            return m;
        }

        public static string Expand(this Step.Interpreter.Step step, Module g)
        {
            string result = null;
            step.Try(TextBuffer.NewEmpty(),
                new BindingEnvironment(g,
                    new MethodCallFrame(null, null, new LogicVariable[0], null, null)),
                (o, u, s, p) =>
                {
                    result = o.AsString;
                    return true;
                },
                null);
            return result;
        }

        public static string Expand(this Step.Interpreter.Step step)
        {
            return step.Expand(new Module("test"));
        }

        internal static Step.Interpreter.Step Sequence(params object[] steps)
        {
            Step.Interpreter.Step next = null;
            for (var i = steps.Length - 1; i >= 0; i--)
            {
                var step = steps[i];
                switch (step)
                {
                    case string[] tokens:
                        next = new EmitStep(tokens, next);
                        break;

                    case object[] call:
                        next = new Call(call[0], call.Skip(1).ToArray(), next);
                        break;

                    default:
                        throw new ArgumentException($"Unknown step argument in Step.Sequence: {step}");
                }
            }

            return next;
        }
    }
}
