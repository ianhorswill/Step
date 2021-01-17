using System;
using Step.Parser;
using Step.Utilities;

namespace Step.Interpreter
{
    internal class AssignmentStep : Step
    {
        public readonly StateVariableName GlobalVariable;
        public readonly LocalVariableName LocalVariable;
        public readonly FunctionalExpression Value;

        public AssignmentStep(StateVariableName globalVariable, FunctionalExpression value, Step next)
        : base(next)
        {
            GlobalVariable = globalVariable;
            LocalVariable = null;
            Value = value;
        }

        public AssignmentStep(LocalVariableName localVariable, FunctionalExpression value, Step next)
            : base(next)
        {
            GlobalVariable = null;
            LocalVariable = localVariable;
            Value = value;
        }

        public override bool Try(PartialOutput output, BindingEnvironment e, Continuation k,
            MethodCallFrame predecessor)
        {
            if (LocalVariable == null)
                return Continue(output,
                    new BindingEnvironment(e,
                        e.Unifications,
                        e.State.Bind(GlobalVariable, Value.Eval(e))),
                    k, predecessor);

            if (e.Unify(LocalVariable, Value.Eval(e), out var result))
                return Continue(output,
                    new BindingEnvironment(e, result, e.State),
                    k, predecessor);
            
            return false;
        }
    }
}
