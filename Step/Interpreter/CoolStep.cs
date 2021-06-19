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

        private CoolStep(int duration, Step next) : base(next)
        {
            Duration = duration;
        }

        /// <summary>
        /// Call number for the task of which this is a part at which this step will become runnable again
        /// </summary>
        private static readonly DictionaryStateElement<CoolStep, int> ReadyTimes =
            new DictionaryStateElement<CoolStep, int>(nameof(ReadyTimes));

        /// <summary>
        /// Call number (for enclosing task) at which this step will be runnable again
        /// </summary>
        public int ReadyTime(State s) => ReadyTimes.GetValueOrDefault(s, this);

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
            var s = e.State;
            var callCount = e.Frame.Task.CallCount(s);
            var readyTime = ReadyTime(s);

            if (callCount > readyTime)
            {
                var newState = ReadyTimes.SetItem(s, this, Duration == int.MaxValue ? int.MaxValue : (callCount + Duration));
                if (Continue(output, new BindingEnvironment(e, e.Unifications, newState), k, predecessor))
                    return true;
            }

            return false;
        }

        public override string Source => "[cool]";
    }
}
