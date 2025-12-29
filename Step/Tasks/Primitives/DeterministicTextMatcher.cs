using Step.Binding;
using Step.Exceptions;
using Step.Interpreter;
using Step.Output;
using Step.Terms;
using System;

namespace Step.Tasks.Primitives
{
    /// <summary>
    /// A primitive that generates/matches its argument as fixed text
    /// </summary>
    public class DeterministicTextMatcher : PrimitiveTask
    {
        /// <summary>
        /// Make a new primitive that generates/matches its argument as text, and succeeds once
        /// </summary>
        /// <param name="name">Name of the primitive</param>
        /// <param name="renderer">Procedure to map argument to desired text.</param>
        public DeterministicTextMatcher(string name, Func<object?, BindingList?, string[]> renderer) : base(name, 1)
        {
            this.renderer = renderer;
        }

        private readonly Func<object?, BindingList?, string[]> renderer;

        /// <inheritdoc />
        public override bool Call(object?[] args, TextBuffer buffer, BindingEnvironment env,
            MethodCallFrame? predecessor, Continuation k)
        {
            ArgumentCountException.Check(Name, 1, args, buffer);
            var arg = env.Resolve(args[0]);
            var text = arg as string[];
            if (buffer.WriteMode)
            {
                if (text == null)
                    text = renderer(arg, env.Unifications);

                return k(buffer.Append(text), env.Unifications, env.State, predecessor);
            }

            // Read mode
            if (arg is LogicVariable l)
            {
                var token = buffer.NextToken(out var newBuffer);
                if (token == null)
                    return false;
                object? value = token;
                switch (token)
                {
                    case "null":
                        value = null;
                        break;
                    case "true":
                        value = true;
                        break;
                    case "false":
                        value = false;
                        break;
                    default:
                        if (int.TryParse(token, out var iValue))
                            value = iValue;
                        else if (float.TryParse(token, out var fValue))
                            value = fValue;
                        break;
                }
                return k(newBuffer,
                    BindingList.Bind(env.Unifications, l, value),
                    env.State, predecessor);
            }

            if (text == null)
                text = renderer(arg, env.Unifications);
            return buffer.Unify(text, out var result)
                   && k(result, env.Unifications, env.State, predecessor);
        }
    }
}
