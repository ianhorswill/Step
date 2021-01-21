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

        public CoolStep(int duration, Step next) : base(next)
        {
            Duration = duration;
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
