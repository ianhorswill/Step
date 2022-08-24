using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Step
{
    /// <summary>
    /// LISP-style cons cells for lists.
    /// </summary>
    public class Cons : IList, IList<object?>
    {
        /// <summary>
        /// First element of the list
        /// </summary>
        public readonly object? First;
        /// <summary>
        /// Rest of the list
        /// </summary>
        public readonly Cons Rest;

        /// <summary>
        /// Represents the empty list
        /// </summary>
        public static readonly Cons Empty = new Cons(null, null!);


        /// <summary>
        /// Make a list with a new element at a beginning
        /// </summary>
        public Cons(object? first, Cons rest)
        {
            First = first;
            Rest = rest;
        }

        /// <summary>
        /// Enumerates the elements of the list
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            for (var cell = this; cell != Empty; cell = cell.Rest)
                yield return cell.First;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<object?> IEnumerable<object?>.GetEnumerator()
        {
            for (var cell = this; cell != Empty; cell = cell.Rest)
                yield return cell.First;
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

        /// <inheritdoc />
        public void CopyTo(Array array, int arrayIndex)
        {
            foreach (var e in this)
                array.SetValue(e, arrayIndex++);
        }

        bool ICollection<object?>.Remove(object? item)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                var c = 0;
                foreach (var unused in this) c++;
                return c;
            }
        }

        /// <inheritdoc />
        public object SyncRoot => throw new NotImplementedException();

        /// <inheritdoc />
        public bool IsSynchronized => false;

        /// <inheritdoc />
        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Contains(object? value) => IndexOf(value) >= 0;

        /// <inheritdoc />
        public void CopyTo(object?[] array, int arrayIndex)
        {
            for (var cell = this; cell != Empty; cell = cell.Rest)
                array[arrayIndex++] = cell.First;
        }

        void ICollection<object?>.Add(object? item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Clear()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public int IndexOf(object? value)
        {
            var cell = this;
            for (var i = 0; cell != Empty; i++)
            {
                if (cell.First == null)
                {
                    if (value == null)
                        return i;
                }
                else if (cell.First.Equals(value))
                    return i;
                else
                    cell = cell.Rest;
            }

            return -1;
        }

        /// <inheritdoc />
        public void Insert(int index, object? value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public object? this[int index]
        {
            get
            {
                var cell = this;
                for (var i = 0; i < index; i++)
                    cell = cell.Rest;
                return cell.First;
            }
            set => throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool IsReadOnly => true;

        /// <inheritdoc />
        public bool IsFixedSize => true;
    }
}
