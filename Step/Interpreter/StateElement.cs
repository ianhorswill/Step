using System;
using System.Diagnostics;
using Step.Serialization;

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
        /// Make a new StateElement with the specified name
        /// </summary>
        public StateElement(string name)
        {
            Name = name;
        }

        /// <summary>
        /// True if this state element has a default value
        /// </summary>
        public virtual bool HasDefault => false;

        /// <summary>
        /// Default value of state element if HasDefault is true
        /// </summary>
        public virtual object? DefaultValue => throw new NotImplementedException();

        /// <inheritdoc />
        public override string ToString() => Name;

        /// <summary>
        /// Method to use to serialize the value of this state element
        /// </summary>
        public virtual void ValueSerializer(Serializer s, object? value) => s.Serialize(value);

        /// <summary>
        /// Method to use to deserialize the value of this method.
        /// </summary>
        public virtual object? ValueDeserializer(Deserializer d) => d.Deserialize();
    }
}
