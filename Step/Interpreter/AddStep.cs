namespace Step.Interpreter
{
    internal class AddStep : Step
    {
        private readonly object element;
        private readonly GlobalVariableName listVariable;

        public AddStep(object element, GlobalVariableName listVariable, Step next)
            : base(next)
        {
            this.element = element;
            this.listVariable = listVariable;
        }

        public override bool Try(PartialOutput output, BindingEnvironment e, Continuation k)
        {
            var elt = e.Resolve(element);
            var listValue = e.Resolve(listVariable);

            if (!(listValue is Cons list))
                throw new ArgumentTypeException("add", typeof(Cons), listValue,
                    new[] {"add", elt, listValue});

            return Continue(output,
                new BindingEnvironment(e,
                    e.Unifications, 
                    e.DynamicState.Bind(listVariable, new Cons(elt, list))),
                k);
        }
    }
}
