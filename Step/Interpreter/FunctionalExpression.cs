using Step.Terms;
using System;
using System.Diagnostics;
using System.Text;
using Step.Exceptions;
using Step.Output;
using Step.Binding;

namespace Step.Interpreter
{
    [DebuggerDisplay("{" + nameof(DebugName) + "}")]
    internal abstract class FunctionalExpression
    {
        public abstract object? Eval(BindingEnvironment e, TextBuffer output);

        public abstract void BuildString(StringBuilder b, bool nested);

        public override string ToString()
        {
            var b = new StringBuilder();
            BuildString(b, false);
            return b.ToString();
        }

        private string DebugName => ToString();
    }

    class Constant : FunctionalExpression
    {
        public readonly object? Value;

        public Constant(object? value)
        {
            Value = value;
        }

        public override object? Eval(BindingEnvironment e, TextBuffer output) => Value;

        public override void BuildString(StringBuilder b, bool nested)
        {
            b.Append(Value);
        }
    }

    class VariableReference : FunctionalExpression
    {
        public readonly object Variable;

        public VariableReference(object variable)
        {
            Variable = variable;
        }

        public override object? Eval(BindingEnvironment e, TextBuffer output)
        {
            var value = e.Resolve(Variable);
            if (value is LogicVariable)
                throw new ArgumentInstantiationException("variable reference", e, new[] {value}, output);
            return value;
        }

        public override void BuildString(StringBuilder b, bool nested)
        {
            b.Append(Variable);
        }
    }

    class UnaryOperator : FunctionalExpression
    {
        public readonly FunctionalOperator<Func<object?, TextBuffer, object?>> Operator;
        public readonly FunctionalExpression Arg;

        public UnaryOperator(FunctionalOperator<Func<object?, TextBuffer, object?>> op, FunctionalExpression arg)
        {
            Arg = arg;
            Operator = op;
        }

        public override object? Eval(BindingEnvironment e, TextBuffer output) => Operator.Implementation(Arg.Eval(e, output), output);

        public override void BuildString(StringBuilder b, bool nested)
        {
            if (nested)
                b.Append('(');
            b.Append(Operator.Name);
            Arg.BuildString(b, true);
            if (nested)
                b.Append(')');
        }
    }

    class BinaryOperator : FunctionalExpression
    {
        public readonly FunctionalOperator<Func<object?, object?, TextBuffer, object?>> Operator;
        public readonly FunctionalExpression Arg1;
        public readonly FunctionalExpression Arg2;

        public BinaryOperator(FunctionalOperator<Func<object?, object?, TextBuffer, object?>> op,
            FunctionalExpression arg1, FunctionalExpression arg2)
        {
            Operator = op;
            Arg1 = arg1;
            Arg2 = arg2;
        }

        public override object? Eval(BindingEnvironment e, TextBuffer output) =>
            Operator.Implementation(Arg1.Eval(e, output), Arg2.Eval(e, output), output);

        public override void BuildString(StringBuilder b, bool nested)
        {
            if (nested)
                b.Append('(');

            Arg1.BuildString(b, true);
            b.Append(Operator.Name);
            Arg2.BuildString(b, true);
            if (nested)
                b.Append(')');
        }
    }
}
