using System;
using System.Collections.Generic;
using System.Linq;

namespace Step.Interpreter
{
    internal abstract class BranchingStep : Step
    {
        protected readonly Step[] Branches;
        
        protected BranchingStep(Step[] branches, Step next) : base(next)
        {
            this.Branches = branches;
        }

        internal override IEnumerable<Step> SubSteps()
        {
            foreach (var chain in Branches)
            foreach (var step in chain.ChainSteps)
                yield return step;
            yield return this;
        }

        /// <inheritdoc />
        public override IEnumerable<object> Callees => Branches.SelectMany(s => s.CalleesOfChain);

        /// <inheritdoc />
        internal override IEnumerable<Call> Calls => Branches.SelectMany(s => s.CallsOfChain);

        public override bool AnyStep(Predicate<Step> p)
        {
            return Branches.Any(branch => branch != null && branch.AnyStep(p)) || base.AnyStep(p);
        }
    }
}
