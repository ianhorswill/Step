using System;
using System.Collections.Immutable;
using System.Linq;
using Step.Parser;

namespace Step.Interpreter
{
    internal class AddStep : Step
    {
        private readonly object? element;
        private readonly StateVariableName collectionVariable;

        private AddStep(object? element, StateVariableName collectionVariable, Step? next)
            : base(next)
        {
            this.element = element;
            this.collectionVariable = collectionVariable;
        }

        public static void FromExpression(ChainBuilder chain, object?[] expression, string? sourceFile = null, int lineNumber = 0)
        {
            if (expression.Length != 3)
                throw new ArgumentCountException("add", 2, expression.Skip(1).ToArray());
            if (!(expression[2] is string vName && DefinitionStream.IsGlobalVariableName(vName)))
                throw new SyntaxError($"Invalid global variable name in add: {expression[2]}", sourceFile,
                    lineNumber);
            chain.AddStep(new AddStep(chain.Canonicalize(expression[1]), StateVariableName.Named(vName), null));
        }


        public override bool Try(TextBuffer output, BindingEnvironment e, Continuation k, MethodCallFrame? predecessor)
        {
            if (!e.TryCopyGround(e.Resolve(element), out var elt))
                throw new ArgumentInstantiationException("add", e,
                new[] {"add", collectionVariable, elt});
            var collectionValue = e.Resolve(collectionVariable);

            switch (collectionValue)
            {
                case Cons list:
                    return Continue(output,
                        new BindingEnvironment(e,
                            e.Unifications,
                            e.State.Bind(collectionVariable, new Cons(elt, list))),
                        k, predecessor);

                case IImmutableSet<object?> set:
                    return Continue(output,
                        new BindingEnvironment(e,
                            e.Unifications,
                            e.State.Bind(collectionVariable, set.Add(elt))),
                        k, predecessor);

                case ImmutableStack<object?> stack:
                    return Continue(output,
                        new BindingEnvironment(e,
                            e.Unifications,
                            e.State.Bind(collectionVariable, stack.Push(elt))),
                        k, predecessor);

                case ImmutableQueue<object?> queue:
                    return Continue(output,
                        new BindingEnvironment(e,
                            e.Unifications,
                            e.State.Bind(collectionVariable, queue.Enqueue(elt))),
                        k, predecessor);

                case ImmutableSortedSet<(object element,float priority)> heap:
                    var pair = elt as object[];
                    if (pair == null)
                        throw new ArgumentTypeException("add", typeof(object[]), elt,
                            new[] { "add", elt, collectionValue });
                    if (pair.Length != 2)
                        throw new ArgumentException(
                            "When adding to a priority queue, the value given should be a two-element tuple");
                    return Continue(output,
                        new BindingEnvironment(e,
                            e.Unifications,
                            e.State.Bind(collectionVariable, heap.Add((pair[0], Convert.ToSingle(pair[1]))))),
                        k, predecessor);

                default:
                    throw new ArgumentTypeException("add", typeof(Cons), collectionValue,
                        new[] { "add", elt, collectionValue });
            }
        }

        public override string Source => "[add ...]";
    }
}
