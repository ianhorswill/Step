using System.Collections.Immutable;

namespace Step.Interpreter
{
    internal class AddStep : Step
    {
        private readonly object element;
        private readonly StateVariableName collectionVariable;

        public AddStep(object element, StateVariableName collectionVariable, Step next)
            : base(next)
        {
            this.element = element;
            this.collectionVariable = collectionVariable;
        }

        public override bool Try(PartialOutput output, BindingEnvironment e, Continuation k)
        {
            var elt = e.Resolve(element);
            var collectionValue = e.Resolve(collectionVariable);

            switch (collectionValue)
            {
                case Cons list:
                    return Continue(output,
                        new BindingEnvironment(e,
                            e.Unifications,
                            e.State.Bind(collectionVariable, new Cons(elt, list))),
                        k);

                case IImmutableSet<object> set:
                    return Continue(output,
                        new BindingEnvironment(e,
                            e.Unifications,
                            e.State.Bind(collectionVariable, set.Add(elt))),
                        k);

                case ImmutableStack<object> stack:
                    return Continue(output,
                        new BindingEnvironment(e,
                            e.Unifications,
                            e.State.Bind(collectionVariable, stack.Push(elt))),
                        k);

                case ImmutableQueue<object> queue:
                    return Continue(output,
                        new BindingEnvironment(e,
                            e.Unifications,
                            e.State.Bind(collectionVariable, queue.Enqueue(elt))),
                        k);

                default:
                    throw new ArgumentTypeException("add", typeof(Cons), collectionValue,
                        new[] { "add", elt, collectionValue });
            }
        }
    }
}
