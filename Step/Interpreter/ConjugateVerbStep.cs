using System;

namespace Step.Interpreter
{
    internal class ConjugateVerbStep : Step
    {
        public ConjugateVerbStep(Step next) : base(next)
        { }

        static readonly GlobalVariableName Tps = GlobalVariableName.Named("ThirdPersonSingular");

        public override bool Try(PartialOutput output, BindingEnvironment e, Continuation k)
        {
            
            var tps = BindingList<GlobalVariableName>.Lookup(e.DynamicState, Tps, true);
            if (!(tps is bool b))
                throw new ArgumentException($"The Plural variable's value is {tps}, but must be a Boolean");

            if (b)
            {
                // We're generating third person singular, so add an s.
                var lastIndex = output.Length - 1;
                var lastWord = output.Buffer[lastIndex];
                output.Buffer[lastIndex] = lastWord + "s";
                if (Continue(output, e, k))
                    return true;
                // Undo change to word
                // Probably not necessary, but you never know.
                output.Buffer[lastIndex] = lastWord;
                return false;
            }
            // We're not generating TPS.
            return Continue(output, e, k);
        }
    }
}
