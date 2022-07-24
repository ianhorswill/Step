namespace Step.Interpreter
{
    /// <summary>
    /// The most general version of a primitive task.
    /// This exposes the internal call structure of the interpreter, so avoid using it unless you have to.
    /// </summary>
    public class GeneralPrimitive : PrimitiveTask
    {
        /// <summary>
        /// Implementation of primitive in terms of the general internal call interface used by the interpreter.
        /// Don't use this unless you know what you're doing.
        /// </summary>
        /// <param name="args">Raw, unevaluated arguments to the task</param>
        /// <param name="o">Partial output accumulated so far</param>
        /// <param name="e">Binding environment to use</param>
        /// <param name="predecessor">Frame of the call that most recently succeeded</param>
        /// <param name="k">Continuation to call when successful</param>
        public delegate bool Implementation(object[] args, TextBuffer o, BindingEnvironment e,
            MethodCallFrame predecessor, Step.Continuation k);

        private readonly Implementation implementation;

        /// <summary>
        /// Make a new primitive task based on an delegate to implement it.
        /// </summary>
        /// <param name="name">Name of the task to be printed by the pretty printer</param>
        /// <param name="implementation">Delegate that implements the task</param>
        public GeneralPrimitive(string name, Implementation implementation) : base(name, null)
        {
            this.implementation = implementation;
        }

        /// <inheritdoc />
        public override bool Call(object[] arglist, TextBuffer output, BindingEnvironment env, MethodCallFrame predecessor, Step.Continuation k)
            => implementation(env.ResolveList(arglist), output, env, predecessor, k);
    }
}
