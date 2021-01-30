using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Step.Interpreter;

namespace Step
{
    /// <summary>
    /// Methods related to testing data-values in logic-specific ways
    /// Term is the word used in logic for something that can be an argument to a predicate.
    /// </summary>
    public abstract class Term
    {
        /// <summary>
        /// True if the tuple contains no variables.
        /// This assumes that the value has already been resolved relative to an environment
        /// so any logic variables are uninstantiated.
        /// </summary>
        public static bool IsGround(object[] tuple)
        {
            foreach (var e in tuple)
                if (e is object[] subArray)
                {
                    if (!IsGround(subArray))
                        return false;
                }
                else if (e is LogicVariable)
                    return false;

            return true;
        }

        /// <summary>
        /// True if object is *not* a logic variable or array containing a logic variable.
        /// This assumes that the value has already been resolved relative to an environment
        /// so any logic variables are uninstantiated.
        /// </summary>
        public static bool IsGround(object o)
        {
            if (o is object[] tuple)
                return IsGround(tuple);
            return !(o is LogicVariable);
        }

        /// <summary>
        /// IEqualityComparer for terms in the step language.
        /// This does recursive comparison and hashing for object[] values, and the default
        /// comparison and hash implementations for others.
        /// </summary>
        public class Comparer : IEqualityComparer<object>
        {
            /// <summary>
            /// Singleton instance of the comparer 
            /// </summary>
            public static readonly Comparer Default = new Comparer();
            
            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                if (x.Equals(y)) return true;
                if (!(x is object[] xArray) || !(y is object[] yArray) || xArray.Length != yArray.Length)
                    return false;
                for (var i = 0; i < xArray.Length; i++)
                    if (!Equals(xArray[i], yArray[i]))
                        return false;
                return true;
            }

            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                if (obj is object[] tuple)
                    return TreeHash(tuple);
                return obj.GetHashCode();
            }

            static int TreeHash(object[] tuple)
            {
                var h = 0;
                foreach (var e in tuple)
                    if (e is object[] subTuple)
                        h ^= TreeHash(subTuple);
                    else h ^= e.GetHashCode();
                return h;
            }
        }
    }
}
