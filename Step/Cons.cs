using System.Collections;
using System.Collections.Generic;

namespace Step
{
    /// <summary>
    /// LISP-style cons cells for lists.
    /// </summary>
    public class Cons : IEnumerable<object>
    {
        /// <summary>
        /// First element of the list
        /// </summary>
        public readonly object First;
        /// <summary>
        /// Rest of the list
        /// </summary>
        public readonly Cons Rest;

        /// <summary>
        /// Represents the empty list
        /// </summary>
        public static readonly Cons Empty = new Cons(null, null);

        /// <inheritdoc />
        public Cons(object first, Cons rest)
        {
            First = first;
            Rest = rest;
        }

        /// <inheritdoc />
        public IEnumerator<object> GetEnumerator()
        {
            for (var cell = this; cell != Empty; cell = cell.Rest)
                yield return cell.First;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
