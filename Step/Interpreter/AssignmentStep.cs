using System;

namespace Step.Interpreter
{
    internal class AssignmentStep : Step
    {
        public readonly StateVariableName Variable;
        public readonly object Value;

        public AssignmentStep(StateVariableName variable, object value, Step next)
        : base(next)
        {
            Variable = variable;
            Value = value;
        }

        public override bool Try(PartialOutput output, BindingEnvironment e, Continuation k, MethodCallFrame predecessor)
        {
            var value = e.Resolve(Value);
            if (value is LogicVariable)
                throw new ArgumentException($"Attempt to Set {Variable.Name} to uninstantiated variable {value}");
            return Continue(output,
                new BindingEnvironment(e,
                    e.Unifications,
                    e.State.Bind(Variable, value)),
                k, predecessor);
        }
    }
}
