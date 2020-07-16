using System.Collections;
using System.Collections.Generic;
using System.Text;

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

        /// <inheritdoc />
        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append('(');

            var count = 0;
            foreach (var e in this)
            {
                if (count == 10)
                {
                    b.Append(" ...");
                    break;
                }
                if (count++ != 0)
                    b.Append(' ');
                b.Append(e);
            }

            b.Append(')');
            return b.ToString();
        }
    }
}
