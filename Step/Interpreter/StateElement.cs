using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public StateElement(string name) => Name = name;

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
