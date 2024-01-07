using System.Collections.Generic;
using System.Collections.Immutable;
using Step.Interpreter;
using Step.Serialization;

namespace Step
{
    /// <summary>
    /// A dictionary whose contents is stored in the global State object
    /// </summary>
    public class DictionaryStateElement<TKey, TValue> : StateElement where TKey:notnull
    {
        /// <summary>
        /// Make a new dictionary that lives in the global state.
        /// Since it lives in the global state, different states will have
        /// different sets of bindings, and making a new binding will
        /// create a new global state.
        /// </summary>
        /// <param name="name">Name to give to the dictionary</param>
        /// <param name="keyComparer">Equality comparer for to use for keys</param>
        /// <param name="valueComparer">Equality comparer to use for values</param>
        public DictionaryStateElement(string name, IEqualityComparer<TKey> keyComparer = null!, IEqualityComparer<TValue> valueComparer = null!)
            : base(name)
        {
            if (keyComparer == null!)
                keyComparer = EqualityComparer<TKey>.Default;
            if (valueComparer == null!)
                valueComparer = EqualityComparer<TValue>.Default;
            
            empty = ImmutableDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer);
        }

        private readonly ImmutableDictionary<TKey, TValue> empty;

        /// <summary>
        /// The version of the dictionary stored in the specified state
        /// </summary>
        private ImmutableDictionary<TKey, TValue>? Dictionary(State state)
        {
            if (state.TryGetValue(this, out var dict))
                return (ImmutableDictionary<TKey, TValue>)dict!;
            return null;
        }

        /// <summary>
        /// Test if the specified Global state contains a binding within this dictionary for the specified key
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public bool ContainsKey(State s, TKey key)
        {
            var dict = Dictionary(s);
            return dict != null && dict.ContainsKey(key);
        }

        /// <summary>
        /// Attempt to find the result associated with this key in the specified state object
        /// </summary>
        /// <param name="state">Global state object to search in</param>
        /// <param name="key">Key to search for</param>
        /// <param name="result">Value associated with key, if any</param>
        /// <returns>True if there is a value associated with the key</returns>
        public bool TryGetValue(State state, TKey key, out TValue result)
        {
            var dict = Dictionary(state);
            if (dict != null && dict.TryGetValue(key, out result!))
                return true;
            result = default!;
            return false;
        }

        /// <summary>
        /// Get the value of the specified key in this dictionary for the specified state, if present.
        /// Otherwise, return the specified default value.
        /// </summary>
        public TValue GetValueOrDefault(State state, TKey key, TValue ifNotFound = default!) => 
            TryGetValue(state, key, out var result) ? result : ifNotFound;

        /// <summary>
        /// Associates a new value to the specified key in this dictionary, in the specified State.
        /// </summary>
        /// <param name="oldState">State before the modification</param>
        /// <param name="key">Key to add</param>
        /// <param name="value">Value to associate with key</param>
        /// <returns>New global state</returns>
        public State SetItem(State oldState, TKey key, TValue value)
        {
            var dict = Dictionary(oldState) ?? empty;
            return oldState.Bind(this, dict.SetItem(key, value));
        }

        /// <summary>
        /// Get all the key/value bindings currently in effect for this dictionary in the specified state.
        /// </summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> Bindings(State s) => Dictionary(s) ?? empty;

        public override void ValueSerializer(Serializer s, object? value) 
            => s.SerializeDictionary((ImmutableDictionary<TKey, TValue>)value!, s.Serialize, s.Serialize);

        public override object ValueDeserializer(Deserializer d)
        {
            var dict = ImmutableDictionary<TKey, TValue>.Empty;
            foreach (var pair in d.DeserializeDictionary(d.Deserialize, d.Deserialize))
                dict = dict.Add((TKey)pair.Key!, (TValue)pair.Value!);
            return dict;
        }
    }
}
