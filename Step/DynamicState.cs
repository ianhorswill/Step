using Step.Interpreter;

namespace Step
{
    /// <summary>
    /// Contains the current dynamic state: the result of any set expressions that have been executed.
    /// </summary>
    public struct DynamicState
    {
        /// <summary>
        /// Binding list for global variables
        /// </summary>
        internal readonly BindingList<GlobalVariableName> Bindings;

        internal DynamicState(BindingList<GlobalVariableName> bindings)
        {
            Bindings = bindings;
        }
    }
}
