using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Step.Binding;

namespace Step.Terms
{
    /// <summary>
    /// A pair used to represent LISP/Prolog-style lists
    /// </summary>
    public class Pair : IEnumerable<object?>
    {
        public readonly object? First;
        public readonly object? Rest;

        public static readonly object Empty = Array.Empty<object?>();

        /// <summary>
        /// A pair used to represent LISP/Prolog-style lists
        /// </summary>
        /// <param name="first">First element of list</param>
        /// <param name="rest">Rest of list</param>
        public Pair(object? first, object? rest)
        {
            First = first;
            Rest = rest;
        }

        /// <summary>
        /// Construct a chain of pairs from an IList.
        /// </summary>
        public static object FromIList(IList list)
        {
            var rest = Empty;
            for (var i = list.Count - 1; i >= 0; i--) 
                rest = new Pair(list[i], rest);
            return rest;
        }

        /// <summary>
        /// Make a chain of pairs with the specified series of elements
        /// </summary>
        public static object List(params object?[] list) => FromIList(list);

        //public static (object? value, bool success) FlattenPairs(object? value, BindingList? u)
        //{
        //    var term = BindingEnvironment.Deref(value, u);
        //    switch (term)
        //    {
        //        case Pair _:
        //            var l = new List<object?>();

        //            while (term is Pair p)
        //            {
        //                l.Add(FlattenPairs(p.First, u));
        //                term = BindingEnvironment.Deref(p.Rest, u);
        //            }

        //            var rest = term as IList;

        //            if (rest == null)
        //                return (null, false);

        //            foreach (var item in rest)
        //                l.Add(item);

        //            return (l.ToArray(), true);

        //        case object?[] array:
        //            return (array.Select(e => FlattenPairs(e, u)).ToArray(), true);

        //        default:
        //            return (term, true);
        //    }
        //}

        /// <summary>
        /// Return the length of the list headed by this pair.
        /// If it is an improper list, return a negative number.
        /// </summary>
        /// <param name="bindings">Bindings currently in effect for local variables</param>
        /// <returns>Length of this, including the tail, if this is a proper list,
        /// or the number of pairs in the list times -1, if the list is improper
        /// (i.e. ends with an unbound variable or something else that isn't a list)</returns>
        public int LengthProperOrImproper(BindingList? bindings)
        {
            var length = 0;
            object? tail = this;
            while (tail is Pair p)
            {
                length++;
                tail = BindingEnvironment.Deref(p.Rest, bindings);
            }

            if (tail is IList list)
                length += list.Count;
            else
                length = -length;

            return length;
        }

        public static object? CompressPairChainsWhenPossible(object? term, BindingList? bindings)
        {
            term = BindingEnvironment.Deref(term, bindings);
            switch (term)
            {
                case Pair p:
                    var l = p.LengthProperOrImproper(bindings);
                    if (l < 0)
                        return new Pair(CompressPairChainsWhenPossible(p.First, bindings),
                            CompressPairChainsWhenPossible(p.Rest, bindings));
                    var array = new object?[l];
                    var i = 0;
                    object? next = p;
                    while (next is Pair nextP)
                    {
                        array[i++] = CompressPairChainsWhenPossible(nextP.First, bindings);
                        next = BindingEnvironment.Deref(nextP.Rest, bindings);
                    }

                    var tail = (IList)next!;
                    foreach (var e in tail)
                        array[i++] = CompressPairChainsWhenPossible(e, bindings);
                    return array;

                case string[] text:
                    return text;

                case object?[] a:
                    var newArray = new object?[a.Length];
                    for (var j = 0; j < a.Length; j++)
                        newArray[j] = CompressPairChainsWhenPossible(a[j], bindings);
                    return newArray;

                default:
                    return term;
            }
        }

        internal void AssertCanonicalEmptyList()
        {
            Debug.Assert(!(Rest is IList l) || l.Count > 0 || ReferenceEquals(Rest, Empty));
        }

        IEnumerator<object?> IEnumerable<object?>.GetEnumerator()
        {
            object? next = this;
            while (next is Pair p)
            {
                yield return p.First;
                next = p.Rest;
            }
            if  (next is IEnumerable e)
                foreach (var item in e)
                    yield return item;
        }

        public IEnumerator GetEnumerator()
        {
            object? next = this;
            while (next is Pair p)
            {
                yield return p.First;
                next = p.Rest;
            }
            if  (next is IEnumerable e)
                foreach (var item in e)
                    yield return item;
        }
    }
}
