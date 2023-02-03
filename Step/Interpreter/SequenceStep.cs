namespace Step.Interpreter
{
    /// <summary>
    /// Implements a sequence in which each successive call invokes the next branch
    /// </summary>
    internal class SequenceStep : BranchingStep
    {
        private readonly StateElement branchNumber = new StateElementWithDefault("sequencePosition", 0);

        /// <summary>
        /// Makes a step that first calls the first branch, then on successive calls, invokes successive branches.
        /// </summary>
        public SequenceStep(Step?[] branches, Step? next) : base(branches, next)
        { }

        /// <summary>
        /// Run the next branch, or fail if we've run out of branches
        /// </summary>
        public override bool Try(TextBuffer output, BindingEnvironment e, Continuation k, MethodCallFrame? predecessor)
        {
            var position = (int)e.State.Lookup(branchNumber)!;
            if (position == Branches.Length)
                return false;
            return Step.Try(Branches[position], output,
                new BindingEnvironment(e, branchNumber, position+1), 
                (o, u, d, newP) =>
                    Continue(o, new BindingEnvironment(e, u, d), k, newP),
                predecessor);
        }

        public override string Source => "[sequence ...]";
    }
}
