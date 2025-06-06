﻿using System.Collections.Immutable;
using System.Linq;
using Step.Parser;

namespace Step.Interpreter
{
    internal class RemoveNextStep : Step
    {
        private readonly object? element;
        private readonly StateVariableName collectionVariable;

        private RemoveNextStep(object? element, StateVariableName collectionVariable, Step? next)
            : base(next)
        {
            this.element = element;
            this.collectionVariable = collectionVariable;
        }

        internal static void FromExpression(ChainBuilder chain, object?[] expression, string? sourceFile = null, int lineNumber = 0)
        {
            if (expression.Length != 3)
                throw new ArgumentCountException("removeNext", 2, expression.Skip(1).ToArray(), new TextBuffer());
            if (!(expression[2] is string vName && DefinitionStream.IsGlobalVariableName(vName)))
                throw new SyntaxError($"Invalid global variable name in add: {expression[2]}", sourceFile,
                    lineNumber);
            chain.AddStep(new RemoveNextStep(chain.Canonicalize(expression[1]), StateVariableName.Named(vName),
                null));
        }


        public override bool Try(TextBuffer output, BindingEnvironment e, Continuation k, MethodCallFrame? predecessor)
        {
            var collectionValue = e.Resolve(collectionVariable);
            var elementVariable = e.Resolve(element);

            switch (collectionValue)
            {
                case Cons list:
                {
                    return list != Cons.Empty 
                        && e.Unify(elementVariable, list.First, out var bindings)
                        && Continue(output,
                            new BindingEnvironment(e,
                                bindings,
                                e.State.Bind(collectionVariable, list.Rest)),
                            k, predecessor);

                }

                case ImmutableStack<object> stack:
                {
                    return !stack.IsEmpty
                           && e.Unify(elementVariable, stack.Peek(), out var bindings)
                           && Continue(output,
                               new BindingEnvironment(e,
                                   bindings,
                                   e.State.Bind(collectionVariable, stack.Pop())),
                               k, predecessor);
                }

                case ImmutableQueue<object> queue:
                {
                    return !queue.IsEmpty
                           && e.Unify(elementVariable, queue.Peek(), out var bindings)
                           && Continue(output,
                               new BindingEnvironment(e,
                                   bindings,
                                   e.State.Bind(collectionVariable, queue.Dequeue())),
                               k, predecessor);
                }

                case ImmutableSortedSet<(object element,float priority)> heap:
                {
                    return !heap.IsEmpty
                           && e.Unify(elementVariable, heap.Max.element, out var bindings)
                           && Continue(output,
                               new BindingEnvironment(e,
                                   bindings,
                                   e.State.Bind(collectionVariable, heap.Remove(heap.Max))),
                               k, predecessor);
                }

                default:
                    throw new ArgumentTypeException("removeNext", typeof(Cons), collectionValue,
                        new[] { "removeNext", collectionValue }, output);
            }
        }

        public override string GetSource(bool markup) => "[removeNext ...]";
    }
}
