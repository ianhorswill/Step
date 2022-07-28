using System.Collections.Generic;
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
        /// Test if two terms are literally equal.  So [?x] and [?x] are literally equal, but [?x] and [?y] are not, even if they are unifiable.
        /// </summary>
        public static bool LiterallyEqual(object a, object b)
        {
            bool Recur(object y, object x)
            {
                if (x.Equals(y)) return true;
                if (!(x is object[] xArray) || !(y is object[] yArray) || xArray.Length != yArray.Length)
                    return false;
                for (var i = 0; i < xArray.Length; i++)
                    if (!Recur(xArray[i], yArray[i]))
                        return false;
                return true;
            }

            return Recur(a, b);
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

            /// <summary>
            /// Comparer to use for cache results on function fluents; ignores the last argument.
            /// </summary>
            public static readonly Comparer ForFunctions = new Comparer() {FunctionComparer = true};

            /// <summary>
            /// True if this is the comparer used for result caches on functions
            /// If so, we need to ignore the last element of a top-level tuple.
            /// </summary>
            public bool FunctionComparer;
            
            bool IEqualityComparer<object>.Equals(object a, object b)
            {
                bool Recur(object y, object x, int ignoredElements = 0)
                {
                    if (x.Equals(y)) return true;
                    if (!(x is object[] xArray) || !(y is object[] yArray) || xArray.Length != yArray.Length)
                        return false;
                    for (var i = 0; i < xArray.Length-ignoredElements; i++)
                        if (!Recur(xArray[i], yArray[i]))
                            return false;
                    return true;
                }

                return Recur(a, b, FunctionComparer?1:0);
            }

            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                if (obj is object[] tuple)
                    return TreeHash(tuple, FunctionComparer ? 1 : 0);
                return obj.GetHashCode();
            }

            static int TreeHash(object[] tuple, int ignoredElements = 0)
            {
                var h = 0;
                for (var index = 0; index < tuple.Length-ignoredElements; index++)
                {
                    var e = tuple[index];
                    if (e is object[] subTuple)
                        h ^= TreeHash(subTuple);
                    else h ^= e.GetHashCode();
                }

                return h;
            }
        }
    }
}
