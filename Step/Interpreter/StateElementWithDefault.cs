using System.Diagnostics;

namespace Step.Interpreter
{
    /// <summary>
    /// A name for an element of the State, that is, something that can change during the execution of the program
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class StateElementWithDefault : StateElement
    {
        public override bool HasDefault => true;

        public override object? DefaultValue { get; }

        /// <summary>
        /// Make a new StateElement with the specified name and default value
        /// </summary>
        public StateElementWithDefault(string name, object? defaultValue) : base(name) => DefaultValue = defaultValue;

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}