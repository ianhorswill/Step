using System;

namespace Step.Interpreter
{
#if ConjugateVerbStep
    // This is deprecated by VerbConjugationFilter
    internal class ConjugateVerbStep : Step
    {
        public ConjugateVerbStep(string suffix, Step next) : base(next)
        {
            Suffix = suffix;
        }

        public readonly string Suffix;

        private static readonly StateVariableName Tps = StateVariableName.Named("ThirdPersonSingular");

        public override bool Try(TextBuffer output, BindingEnvironment e, Continuation k, MethodCallFrame predecessor)
        {
            
            var tps = e.State.LookupOrDefault(Tps, true);
            if (!(tps is bool b))
                throw new ArgumentException($"The Plural variable's value is {tps}, but must be a Boolean");

            if (!b)
                // We're not generating TPS.
                return Continue(output, e, k, predecessor);

            // We're generating third person singular, so add an s.
            var lastIndex = output.Length - 1;
            var lastWord = output.Buffer[lastIndex];
            if (Suffix == "es" && lastWord.EndsWith("y"))
                output.Buffer[lastIndex] = lastWord.Substring(0, lastWord.Length-1) + "ies";
            else
                output.Buffer[lastIndex] = lastWord + Suffix;
            if (Continue(output, e, k, predecessor))
                return true;
            // Undo change to word
            // Probably not necessary, but you never know.
            output.Buffer[lastIndex] = lastWord;
            return false;
        }

        public override string Source => "[s]";
    }
#endif
}
