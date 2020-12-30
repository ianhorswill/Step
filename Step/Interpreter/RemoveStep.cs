using System.Collections.Immutable;

namespace Step.Interpreter
{
    internal class RemoveNextStep : Step
    {
        private readonly object element;
        private readonly StateVariableName collectionVariable;

        public RemoveNextStep(object element, StateVariableName collectionVariable, Step next)
            : base(next)
        {
            this.element = element;
            this.collectionVariable = collectionVariable;
        }

        public override bool Try(PartialOutput output, BindingEnvironment e, Continuation k, MethodCallFrame predecessor)
        {
            var collectionValue = e.Resolve(collectionVariable);

            switch (collectionValue)
            {
                case Cons list:
                {
                    return list != Cons.Empty 
                        && e.Unify(element, list.First, out var bindings)
                        && Continue(output,
                            new BindingEnvironment(e,
                                bindings,
                                e.State.Bind(collectionVariable, list.Rest)),
                            k, predecessor);

                }

                case ImmutableStack<object> stack:
                {
                    return !stack.IsEmpty
                           && e.Unify(element, stack.Peek(), out var bindings)
                           && Continue(output,
                               new BindingEnvironment(e,
                                   bindings,
                                   e.State.Bind(collectionVariable, stack.Pop())),
                               k, predecessor);
                }

                case ImmutableQueue<object> queue:
                {
                    return !queue.IsEmpty
                           && e.Unify(element, queue.Peek(), out var bindings)
                           && Continue(output,
                               new BindingEnvironment(e,
                                   bindings,
                                   e.State.Bind(collectionVariable, queue.Dequeue())),
                               k, predecessor);
                }

                default:
                    throw new ArgumentTypeException("removeNext", typeof(Cons), collectionValue,
                        new[] { "removeNext", collectionValue });
            }
        }
    }
}
