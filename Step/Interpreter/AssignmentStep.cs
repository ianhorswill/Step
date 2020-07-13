using System;

namespace Step.Interpreter
{
    internal class AssignmentStep : Step
    {
        public readonly GlobalVariableName Variable;
        public readonly object Value;

        public AssignmentStep(GlobalVariableName variable, object value, Step next)
        : base(next)
        {
            Variable = variable;
            Value = value;
        }

        public override bool Try(PartialOutput output, BindingEnvironment e, Continuation k)
        {
            var value = e.Resolve(Value);
            if (value is LogicVariable)
                throw new ArgumentException($"Attempt to Set {Variable.Name} to uninstantiated variable {value}");
            return Continue(output,
                new BindingEnvironment(e,
                    e.Unifications,
                    BindingList<GlobalVariableName>.Bind(e.DynamicState, Variable, value)),
                k);
        }
    }
}
