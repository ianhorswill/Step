using System;
using System.Collections.Generic;
using System.Linq;

namespace Step.Interpreter
{
    internal abstract class BranchingStep : Step
    {
        protected readonly Step?[] Branches;
        
        protected BranchingStep(Step?[] branches, Step? next) : base(next)
        {
            this.Branches = branches;
        }

        internal override IEnumerable<Step> SubSteps()
        {
            foreach (var chain in Branches)
            foreach (var step in ChainSteps(chain))
                yield return step;
            yield return this;
        }

        /// <inheritdoc />
        public override IEnumerable<object> Callees => Branches.SelectMany(s => CalleesOfChain(s));

        /// <inheritdoc />
        internal override IEnumerable<Call> Calls => Branches.SelectMany(s => CallsOfChain(s));

        public override bool AnyStep(Predicate<Step> p)
        {
            return Branches.Any(branch => branch != null && branch.AnyStep(p)) || base.AnyStep(p);
        }
    }
}
