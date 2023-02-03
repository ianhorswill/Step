using System;
using System.Collections.Generic;
using Step.Interpreter;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Step.Serialization;

namespace Step
{
    /// <summary>
    /// Contains the current dynamic state: the result of any set expressions that have been executed,
    /// or any other state changes that might need to be undone upon backtracking
    /// </summary>
    public readonly struct State : ISerializable
    {
        static State()
        {
            Deserializer.RegisterHandler(typeof(State), Deserialize);
        }
        /// <summary>
        /// Binding list for global variables
        /// </summary>
        internal readonly ImmutableDictionary<StateElement, object?> Bindings;

        private State(ImmutableDictionary<StateElement, object?> bindings)
        {
            Bindings = bindings;
        }

        /// <summary>
        /// Binds the specified state element to the specified value
        /// </summary>
        /// <returns>New dynamic state</returns>
        public State Bind(StateElement e, object? value) => new State(Bindings.SetItem(e, value));

        /// <summary>
        /// The value of the specified dynamic state element
        /// </summary>
        public object? this[StateElement e] => Lookup(e);

        /// <summary>
        /// The value of the specified dynamic state element
        /// </summary>
        public object? Lookup(StateElement e)
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
        public object? LookupOrDefault(StateElement e, object defaultValue)
            => Bindings.TryGetValue(e, out var result) ? result : defaultValue;

        /// <summary>
        /// Set result to the value of e and return true, if e is bound, else return false
        /// </summary>
        public bool TryGetValue(StateElement e, out object? result) => Bindings.TryGetValue(e, out result);

        /// <summary>
        /// A State containing no bindings
        /// </summary>
        public static readonly State Empty =
            new State(ImmutableDictionary<StateElement, object?>.Empty);

        /// <summary>
        /// Returns contents as a flat array, sorted.
        /// Not performant - just use for examining the contents in the debugger.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public KeyValuePair<string, object?>[] Contents
        {
            get
            {
                if (Bindings == null)
                    return Array.Empty<KeyValuePair<string, object?>>();
                
                var bindings = Bindings.Select(p => new KeyValuePair<string, object?>(p.Key.Name, p.Value)).ToArray(); 
                Array.Sort(bindings,
                    (a, b) => String.CompareOrdinal(a.Key, b.Key));
                return bindings;
            }

        }

        public void Serialize(Serializer s)
        {
            foreach (var pair in Bindings)
            {
                s.Write(' ');
                s.Serialize(pair.Key);
                s.Write("=");
                pair.Key.ValueSerializer(s, pair.Value);
            }
        }

        (char start, string typeToken, char end, bool includeSpace) ISerializable.SerializationBracketing() => ('{', "State", '}', false);

        private static object Deserialize(Deserializer d)
        {
            var s = State.Empty;
            d.SkipWhitespace();
            while (d.Peek() != '}')
            {
                var key = d.Expect<StateElement>();
                var equals = (char)d.Read();
                if (equals != '=')
                    throw new InvalidDataException($"Expected '=' after state element {key} but got {equals}");
                var value = key.ValueDeserializer(d);
                s = s.Bind(key, value);
                d.SkipWhitespace();
            }

            return s;
        }

        public static bool TestEqual(State a, State b)
        {
            bool Compare(State x, State y)
            {
                foreach (var pair in x.Bindings)
                    if (!Equals(pair.Value, y[pair.Key]))
                        return false;
                return true;
            }

            return Compare(a, b) && Compare(b, a);
        }
    }
}
