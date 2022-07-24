using System.Linq;
using Step.Output;
using Step.Parser;

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
            if (!e.TryCopyGround(Value.Eval(e), out var expValue))
                // You can't set a variable to a non-ground value
                throw new ArgumentInstantiationException("set", e,
                    new[] { (object)GlobalVariable, Value});

            if (LocalVariable == null)
                return Continue(output,
                    new BindingEnvironment(e,
                        e.Unifications,
                        e.State.Bind(GlobalVariable, expValue)),
                    k, predecessor); 

            if (e.Unify(LocalVariable, expValue, out var result))
                return Continue(output,
                    new BindingEnvironment(e, result, e.State),
                    k, predecessor);
            
            return false;
        }

        internal static void FromExpression(ChainBuilder chain, object[] expression, 
            string sourceFile = null, int lineNumber = 0)
        {
            switch ((string) expression[0])
            {
                case "set":
                case "now":
                    FromSetExpression(chain, expression, sourceFile, lineNumber);
                    break;

                case "inc": 
                case "dec":
                    FromIncDecExpression(chain, expression, sourceFile, lineNumber);
                    break;
            }
        }

        private static void FromSetExpression(ChainBuilder chain, object[] expression, string sourceFile, int lineNumber)
        {
            if (expression.Length < 4 || !expression[2].Equals("=") || !(expression[1] is string name))
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

        private static void FromIncDecExpression(ChainBuilder chain, object[] expression, string sourceFile, int lineNumber)
        {
            if (expression.Length < 2 || !(expression[1] is string name))
                throw new SyntaxError(
                    $"An inc or dec command has the format [inc name] or [inc name amount], which doesn't match the expression {Writer.TermToString(expression)}.",
                    sourceFile, lineNumber);

            var operation = expression[0].Equals("inc") ? "+" : "-";
            var increment = (expression.Length == 2) ? new object[] {1} : expression.Skip(2).Prepend("(").Append(")");
            var newValueExpression = new object[]
            {
                name,
                operation
            }.Concat(increment).ToArray();
            
            if (DefinitionStream.IsGlobalVariableName(name))
                chain.AddStep(new AssignmentStep(StateVariableName.Named(name),
                    FunctionalExpressionParser.FromTuple(chain.CanonicalizeArglist(newValueExpression), 0, sourceFile,
                        lineNumber),
                    null));
            else if (DefinitionStream.IsLocalVariableName(name))
                throw new SyntaxError(
                    $"An inc or dec command cannot be used with the local variable {name} because local variables can only be set once.",
                    sourceFile, lineNumber);
            else
                throw new SyntaxError(
                    $"An inc or dec command can only update a variable; it can't update {expression[1]}",
                    sourceFile, lineNumber);
        }

        public override string Source => "[set ...]";
    }
}
