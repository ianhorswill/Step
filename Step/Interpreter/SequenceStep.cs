namespace Step.Interpreter
{
    /// <summary>
    /// Implements a sequence in which each successive call invokes the next branch
    /// </summary>
    public class SequenceStep : Step
    {
        private readonly Step[] branches;
        private readonly StateElement branchNumber = new StateElement("sequencePosition", true, 0);

        /// <summary>
        /// Makes a step that first calls the first branch, then on successive calls, invokes successive branches.
        /// </summary>
        public SequenceStep(Step[] branches, Step next) : base(next)
        {
            this.branches = branches;
        }

        /// <summary>
        /// Run the next branch, or fail if we've run out of branches
        /// </summary>
        public override bool Try(PartialOutput output, BindingEnvironment e, Continuation k)
        {
            var position = (int)e.State.Lookup(branchNumber);
            if (position == branches.Length)
                return false;
            return branches[position].Try(output,
                new BindingEnvironment(e, branchNumber, position+1), 
                (o, u, d) =>
                    Continue(o, new BindingEnvironment(e, u, d), k));
        }
    }
}
