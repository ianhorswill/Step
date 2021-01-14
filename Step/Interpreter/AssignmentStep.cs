using System;

namespace Step.Interpreter
{
    internal class AssignmentStep : Step
    {
        public readonly StateVariableName Variable;
        public readonly FunctionalExpression Value;

        public AssignmentStep(StateVariableName variable, FunctionalExpression value, Step next)
        : base(next)
        {
            Variable = variable;
            Value = value;
        }

        public override bool Try(PartialOutput output, BindingEnvironment e, Continuation k, MethodCallFrame predecessor) =>
            Continue(output,
                new BindingEnvironment(e,
                    e.Unifications,
                    e.State.Bind(Variable, Value.Eval(e))),
                k, predecessor);
    }
}
