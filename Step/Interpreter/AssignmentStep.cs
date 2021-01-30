using Step.Parser;
using Step.Utilities;

namespace Step.Interpreter
{
    internal class AssignmentStep : Step
    {
        public readonly StateVariableName GlobalVariable;
        public readonly LocalVariableName LocalVariable;
        public readonly FunctionalExpression Value;

        private AssignmentStep(StateVariableName globalVariable, FunctionalExpression value, Step next)
        : base(next)
        {
            GlobalVariable = globalVariable;
            LocalVariable = null;
            Value = value;
        }

        private AssignmentStep(LocalVariableName localVariable, FunctionalExpression value, Step next)
            : base(next)
        {
            GlobalVariable = null;
            LocalVariable = localVariable;
            Value = value;
        }

        public override bool Try(TextBuffer output, BindingEnvironment e, Continuation k,
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


        internal static void FromExpression(ChainBuilder chain, object[] expression, 
            string sourceFile = null, int lineNumber = 0)
        {
            if (expression.Length < 4)
                throw new SyntaxError(
                    $"A set command has the format [set name = value], which doesn't match the expression {Writer.TermToString(expression)}.",
                    sourceFile, lineNumber);
            if (!expression[2].Equals("="))
                throw new SyntaxError(
                    $"A set command has the format [set name = value], but this expression has {Writer.TermToString(expression[2])} instead of =.",
                    sourceFile, lineNumber);

            if (!(expression[1] is string name))
                throw new SyntaxError(
                    $"A set command has the format [set name = value], which doesn't match the expression {Writer.TermToString(expression)}.",
                    sourceFile, lineNumber);

            if (DefinitionStream.IsGlobalVariableName(name))
                chain.AddStep(new AssignmentStep(StateVariableName.Named(name),
                    FunctionalExpressionParser.FromTuple(chain.CanonicalizeArglist(expression), 3, sourceFile,
                        lineNumber),
                    null));
            else if (DefinitionStream.IsLocalVariableName(name))
                chain.AddStep(new AssignmentStep(chain.GetLocal(name),
                    FunctionalExpressionParser.FromTuple(chain.CanonicalizeArglist(expression), 3, sourceFile,
                        lineNumber),
                    null));
            else
                throw new SyntaxError(
                    $"A set command can only update a variable; it can't update {expression[1]}",
                    sourceFile, lineNumber);
        }
    }
}
