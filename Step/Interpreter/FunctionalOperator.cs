using System.Diagnostics;

namespace Step.Interpreter
{
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    class FunctionalOperator<TFunction>
    {
        public readonly string Name;
        public readonly int Precedence;
        public readonly TFunction Implementation;

        public FunctionalOperator(string name, int precedence, TFunction implementation)
        {
            Name = name;
            Precedence = precedence;
            Implementation = implementation;
        }
    }
}
