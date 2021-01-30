using System.Linq;
using Step.Parser;

namespace Step.Interpreter

{
    /// <summary>
    /// A cooldown timer that can be placed anyplace in a method.
    /// After succeeding, it will fail for Duration subsequent calls.
    /// </summary>
    internal class CoolStep : Step
    {
        public readonly int Duration;
        private int fuse;

        private CoolStep(int duration, Step next) : base(next)
        {
            Duration = duration;
        }

        internal static void FromOnceExpression(ChainBuilder chain, object[] expression)
        {
            if (expression.Length != 1)
                throw new ArgumentCountException("once", 0, expression.Skip(1).ToArray());

            chain.AddStep(new CoolStep(int.MaxValue, null));
        }

        internal static void FromCoolExpression(ChainBuilder chain, object[] expression, string sourceFile, int lineNumber)
        {
            if (expression.Length > 2)
                throw new ArgumentCountException("cool", 1, expression.Skip(1).ToArray());

            var duration = 1;
            if (expression.Length == 2)
            {
                if (int.TryParse(expression[1] as string, out var d))
                    duration = d;
                else
                    throw new SyntaxError(
                        $"Argument to cool must be an integer constant, but got {expression[1]}", sourceFile,
                        lineNumber);
            }

            chain.AddStep(new CoolStep(duration, null));
        }

        public override bool Try(TextBuffer output, BindingEnvironment e, Continuation k, MethodCallFrame predecessor)
        {
            if (fuse == 0)
            {
                fuse = Duration;
                if (Continue(output, e, k, predecessor))
                    return true;

                // The continuation failed, so the user never saw its results.
                // So un-fire the fuse.
                fuse = 0;
                return false;
            }

            if (fuse != int.MaxValue)
                fuse--;
            return false;
        }
    }
}
