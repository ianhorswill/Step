using System;
using System.Collections.Generic;

namespace Step.Interpreter
{
    /// <summary>
    /// A predicate implemented by scanning a table of tuples
    /// Important: "tuple" here means object[], not the Tuple generic type
    /// </summary>
    public class TuplePredicate : PrimitiveTask
    {
        /// <summary>
        /// Number of arguments to the predicate
        /// This must be the same as the length of every tuple.
        /// </summary>
        public int Arity => Signature.Length;
        /// <summary>
        /// Expected types for each argument
        /// </summary>
        public readonly Type[] Signature;
        /// <summary>
        /// Index for each argument, or null if that argument isn't indexed
        /// An index maps a value for the argument to a list of tuples that match it
        /// </summary>
        private readonly Dictionary<object, List<object[]>>[] indices;
        /// <summary>
        /// The complete list of all tuples
        /// </summary>
        private readonly List<object[]> tuples = new List<object[]>();

        /// <summary>
        /// Procedure too shuffle tuples, if desired
        /// </summary>
        public Func<IList<object[]>, IEnumerable<object[]>> Shuffler = x => x;
        

        /// <summary>
        /// Make a new general predicate that is true whenever the arguments match a tuple in the specified set.
        /// </summary>
        /// <param name="name">Name to give to the predicate</param>
        /// <param name="signature">Array of expected types for the arguments.  Use Object if it can be any type</param>
        /// <param name="indexArgument">Array of Booleans specifying whether to build indices for each argument</param>
        /// <param name="tuples">Generator for the actual tuples to store</param>
        /// <exception cref="ArgumentException">If the signature, indexArgument, or tuple arrays don't all have the same length</exception>
        public TuplePredicate(string name, Type[] signature, bool[] indexArgument, IEnumerable<object[]> tuples)
        : base(name, signature.Length)
        {
            Signature = signature;
            if (indexArgument.Length != Arity)
                throw new ArgumentException("indexArgument array has different length than signature");
            indices = new Dictionary<object, List<object[]>>[Arity];

            for (var i = 0; i < Arity; i++)
                indices[i] = indexArgument[i] ? new Dictionary<object, List<object[]>>() : null;

            foreach (var t in tuples)
            {
                if (t.Length != Arity)
                    throw new ArgumentException("Tuple has different length than signature");
                this.tuples.Add(t);
                for (var i = 0; i < Arity; i++)
                    if (indexArgument[i])
                    {
                        // Add to index
                        var arg = t[i];
                        if (!indices[i].TryGetValue(arg, out var index))
                        {
                            index = new List<object[]>();
                            indices[i][arg] = index;
                        }

                        index.Add(t);
                    }
            }
        }

        /// <inheritdoc />
        public override bool Call(object[] arglist, TextBuffer output, BindingEnvironment env, MethodCallFrame predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(this, Arity, arglist);
            
            // Check types and find the shortest list of candidate tuples that match
            var candidateTuples = tuples;
            for (var i = 0; i < Arity; i++)
            {
                var arg = arglist[i];
                if (arg is LogicVariable)
                    continue;
                ArgumentTypeException.Check(this, Signature[i], arg, arglist);
                var index = indices[i];
                if (index != null)
                {
                    if (!index.TryGetValue(arg, out var indexedTuples))
                        // There are literally no tuples with the specified argument value
                        return false;
                    if (indexedTuples.Count < candidateTuples.Count)
                        candidateTuples = indexedTuples;
                }
            }
            
            // Attempt to unify each candidate tuple with the argument list
            foreach (var t in Shuffler(candidateTuples))
                if (env.UnifyArrays(arglist, t, out BindingList<LogicVariable> unifications))
                    if (k(output, unifications, env.State, predecessor))
                        return true;
            return false;
        }
    }
}
