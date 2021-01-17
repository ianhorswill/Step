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
    }
}
