using System.Diagnostics;

namespace Step.Interpreter
{
    /// <summary>
    /// A name for an element of the State, that is, something that can change during the execution of the program
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class StateElement
    {
        /// <summary>
        /// Name of the dynamic state element.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// True if this state element has a default value
        /// </summary>
        public readonly bool HasDefault;
        /// <summary>
        /// Default value of state element if HasDefault is true
        /// </summary>
        public readonly object DefaultValue;

        /// <summary>
        /// Make a new StateElement with the specified name
        /// </summary>
        public StateElement(string name, bool hasDefault, object defaultValue)
        {
            Name = name;
            HasDefault = hasDefault;
            DefaultValue = defaultValue;
        }

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
