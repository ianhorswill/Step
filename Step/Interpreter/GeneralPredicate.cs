using System;
using System.Collections.Generic;
using Step.Utilities;

namespace Step.Interpreter
{
    /// <summary>
    /// A general unary predicate
    /// Takes one argument, which can be instantiated or not
    /// Can succeed or fail an number of times
    /// Cannot access or modify global state
    /// </summary>
    public abstract class GeneralPredicateBase : PrimitiveTask
    {
        /// <summary>
        /// A statically allocated empty binding list for implementations that don't need to bind anything.
        /// Saves a little bit of memory allocation.
        /// </summary>
        protected static readonly BindingList?[] EmptyBindingListArray = new BindingList?[0];

        /// <summary>
        /// Enumerates the non-deterministic solutions for this particular call to the predicate
        /// </summary>
        /// <param name="args">Arguments in the call</param>
        /// <param name="e">Binding environment</param>
        /// <returns></returns>
        protected abstract IEnumerable<BindingList?> Iterator(object?[] args, BindingEnvironment e);

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            foreach (var bindings in Iterator(arglist, env))
                if (k(output, bindings, env.State, predecessor))
                    return true;
            return false;
        }

        /// <inheritdoc />
        protected GeneralPredicateBase(string name, int? argumentCount) : base(name, argumentCount)
        {
        }
    }

    /// <summary>
    /// A general unary predicate
    /// Takes one argument, which can be instantiated or not
    /// Can succeed or fail an number of times
    /// Cannot access or modify global state
    /// </summary>
    public class GeneralPredicate<T1> : GeneralPredicateBase
    {
        /// <summary>
        /// A general unary predicate
        /// Takes one argument, which can be instantiated or not
        /// Can succeed or fail an number of times
        /// Cannot access or modify global state
        /// </summary>
        /// <param name="name">Name of the primitive</param>
        /// <param name="inMode">Implementation of the predicate for when the argument is instantiated</param>
        /// <param name="outMode">Implementation of the predicate for when the argument is not instantiated</param>
        public GeneralPredicate(string name, Func<T1, bool> inMode, Func<IEnumerable<T1>> outMode) : base(name, 1)
        {
            this.inMode = inMode;
            this.outMode = outMode;
        }

        /// <summary>
        /// Make a unary predicate from an IList.
        /// List should not be modified after this call.
        /// </summary>
        /// <param name="name">Name for the predicate</param>
        /// <param name="list">List containing all elements</param>
        /// <returns>Predicate</returns>
        // ReSharper disable once UnusedMember.Global
        public static GeneralPredicate<T1> FromList(string name, IList<T1> list)
        {
            var hashSet = new HashSet<T1>(list);
            return new GeneralPredicate<T1>(
                name,
                elt => hashSet.Contains(elt),
                () => list);
        }

        /// <summary>
        /// Make a unary predicate from an IList.  Enumerates elements using BadShuffle.
        /// List should not be modified after this call.
        /// </summary>
        /// <param name="name">Name for the predicate</param>
        /// <param name="list">List containing all elements</param>
        /// <returns>Predicate</returns>
        // ReSharper disable once UnusedMember.Global
        public static GeneralPredicate<T1> FromListRandomized(string name, IList<T1> list)
        {
            var hashSet = new HashSet<T1>(list);
            return new GeneralPredicate<T1>(
                name,
                elt => hashSet.Contains(elt),
                list.BadShuffle);
        }

        private readonly Func<T1, bool> inMode;
        private readonly Func<IEnumerable<T1>> outMode;

        /// <inheritdoc />
        protected override IEnumerable<BindingList?> Iterator(object?[] args, BindingEnvironment e)
        {
            ArgumentCountException.Check(Name, 1, args);
            var arg = e.Resolve(args[0]);
            switch (arg)
            {
                case LogicVariable v:
                {
                    foreach (var result in outMode())
                        yield return BindingList.Bind(e.Unifications, v, result);
                    break;
                }
                case T1 value:
                    if (inMode(value))
                        yield return e.Unifications;
                    break;
                default:
                    throw new ArgumentTypeException(Name, typeof(T1), arg, args);
            }
        }
    }

    /// <summary>
    /// A general unary predicate
    /// Takes one argument, which can be instantiated or not
    /// Can succeed or fail an number of times
    /// Cannot access or modify global state
    /// </summary>
    public class GeneralPredicate<T1, T2> : GeneralPredicateBase
    {
        /// <summary>
        /// A general unary predicate
        /// Takes one argument, which can be instantiated or not
        /// Can succeed or fail an number of times
        /// Cannot access or modify global state
        /// </summary>
        /// <param name="name">Name of the predicate</param>
        /// <param name="inInMode">Implementation of the predicate for when both arguments are instantiated</param>
        /// <param name="inOutMode">Implementation for when the first argument is instantiated and the second isn't</param>
        /// <param name="outInMode">Implementation for when only the second argument is instantiated</param>
        /// <param name="outOutMode">Implementation for when neither argument is instantiated</param>
        public GeneralPredicate(string name, Func<T1, T2, bool> inInMode, Func<T1, IEnumerable<T2>>? inOutMode, Func<T2, IEnumerable<T1>>? outInMode, Func<IEnumerable<(T1, T2)>>? outOutMode)
         : base(name, 2)
        {
            this.inInMode = inInMode;
            this.inOutMode = inOutMode;
            this.outInMode = outInMode;
            this.outOutMode = outOutMode;
        }

        private readonly Func<T1, T2, bool> inInMode;
        private readonly Func<T1, IEnumerable<T2>>? inOutMode;
        private readonly Func<T2, IEnumerable<T1>>? outInMode;
        private readonly Func<IEnumerable<(T1, T2)>>? outOutMode;

        /// <inheritdoc />
        protected override IEnumerable<BindingList?> Iterator(object?[] args, BindingEnvironment e)
        {
            ArgumentCountException.Check(Name, 2, args);
            var arg1 = e.Resolve(args[0]);
            var arg2 = e.Resolve(args[1]);
            switch (arg1)
            {
                case LogicVariable v1:
                    switch (arg2)
                    {
                        case LogicVariable v2:
                            if (outOutMode == null)
                                throw new ArgumentInstantiationException(Name, e, args);

                            foreach (var (out1, out2) in outOutMode())
                                yield return BindingList.Bind(e.Unifications, v1, out1).Bind(v2, out2);
                            break;

                        case T2 in2:
                            if (outInMode == null)
                                throw new ArgumentInstantiationException(Name, e, args);

                            foreach (var out1 in outInMode(in2))
                                yield return BindingList.Bind(e.Unifications, v1, out1);
                            break;

                        default:
                            throw new ArgumentTypeException(Name, typeof(T2), arg2, new[] { arg1, arg2 });
                    }

                    break;

                case T1 in1:
                    switch (arg2)
                    {
                        case LogicVariable v2:
                            if (inOutMode == null)
                                throw new ArgumentInstantiationException(Name, e, args);

                            foreach (var out2 in inOutMode(in1))
                                yield return BindingList.Bind(e.Unifications, v2, out2);
                            break;

                        case T2 in2:
                            if (inInMode == null)
                                throw new ArgumentInstantiationException(Name, e, args);

                            if (inInMode(in1, in2))
                                yield return e.Unifications;
                            break;

                        default:
                            throw new ArgumentTypeException(Name, typeof(T2), arg2, new[] { arg1, arg2 });
                    }

                    break;

                default:
                    throw new ArgumentTypeException(Name, typeof(T1), arg1, new[] { arg1, arg2 });
            }
        }
    }

    /// <summary>
    /// Most general interface for writing predicates.
    /// Does no argument checking, and implementation is a mapping from arglists to an enumerations
    /// of arrays to unify the arglist with.
    /// </summary>
    public class GeneralNAryPredicate : GeneralPredicateBase
    {
        /// <summary>
        /// Create a general primitive predicate.
        /// </summary>
        /// <param name="name">Name of the predicate</param>
        /// <param name="implementation">Mapping from arguments for the predicate to arrays with which to unify it.
        /// Returning the empty sequence means failure,
        /// but returning multiple arglists means the predicate can succeed multiple times.
        /// </param>
        public GeneralNAryPredicate(string name, Func<object?[], IEnumerable<object?[]>> implementation) : base(name, null)
        {
            this.implementation = implementation;
        }

        private readonly Func<object?[], IEnumerable<object?[]>> implementation;

        /// <inheritdoc />
        protected override IEnumerable<BindingList?> Iterator(object?[] args, BindingEnvironment e)
        {
            foreach (var result in implementation(e.ResolveList(args))) {
                if (!e.UnifyArrays(args, result, out BindingList? bindings))
                    throw new Exception($"Internal error in {Name}: could not unify result with arguments");
                yield return bindings;
            }
        }
    }
}
