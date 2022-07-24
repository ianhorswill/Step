using System;
using System.Collections.Generic;
using Step.Interpreter;
using System.Collections.Immutable;
using System.Linq;

namespace Step
{
    /// <summary>
    /// Contains the current dynamic state: the result of any set expressions that have been executed,
    /// or any other state changes that might need to be undone upon backtracking
    /// </summary>
    public readonly struct State
    {
        /// <summary>
        /// Binding list for global variables
        /// </summary>
        internal readonly ImmutableDictionary<StateElement, object> Bindings;

        private State(ImmutableDictionary<StateElement, object> bindings)
        {
            Bindings = bindings;
        }

        /// <summary>
        /// Binds the specified state element to the specified value
        /// </summary>
        /// <returns>New dynamic state</returns>
        public State Bind(StateElement e, object value) => new State(Bindings.SetItem(e, value));

        /// <summary>
        /// The value of the specified dynamic state element
        /// </summary>
        public object this[StateElement e] => Lookup(e);

        /// <summary>
        /// The value of the specified dynamic state element
        /// </summary>
        public object Lookup(StateElement e)
        {
            if (Bindings.TryGetValue(e, out var result))
                return result;
            if (e.HasDefault)
                return e.DefaultValue;
            throw new KeyNotFoundException($"State contains no value for state element {e.Name}");
        }

        /// <summary>
        /// The value of the specified dynamic state element or the specified default value, if the state element
        /// has no value.
        /// </summary>
        public object LookupOrDefault(StateElement e, object defaultValue)
            => Bindings.TryGetValue(e, out var result) ? result : defaultValue;

        /// <summary>
        /// Set result to the value of e and return true, if e is bound, else return false
        /// </summary>
        public bool TryGetValue(StateElement e, out object result) => Bindings.TryGetValue(e, out result);

        /// <summary>
        /// A State containing no bindings
        /// </summary>
        public static readonly State Empty =
            new State(ImmutableDictionary<StateElement, object>.Empty);

        /// <summary>
        /// Returns contents as a flat array, sorted.
        /// Not performant - just use for examining the contents in the debugger.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public KeyValuePair<string, object>[] Contents
        {
            get
            {
                if (Bindings == null)
                    return new KeyValuePair<string, object>[0];
                
                var bindings = Bindings.Select(p => new KeyValuePair<string, object>(p.Key.Name, p.Value)).ToArray(); 
                Array.Sort(bindings,
                    (a, b) => String.CompareOrdinal(a.Key, b.Key));
                return bindings;
            }

        }
    }
}
