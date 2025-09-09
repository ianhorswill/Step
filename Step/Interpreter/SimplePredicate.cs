using System;

namespace Step.Interpreter
{
    /// <summary>
    /// A primitive task that implements a deterministic function from arguments to a boolean
    /// Requires all arguments to be instantiated
    /// </summary>
    public class SimplePredicate : PrimitiveTask
    {
        /// <summary>
        /// A primitive task that implements a deterministic function from arguments to a boolean
        /// Requires all arguments to be instantiated
        /// </summary>
        /// <param name="name">Name of the predicate</param>
        /// <param name="implementation">Low-level implementation of the predicate</param>
        public SimplePredicate(string name, Func<bool> implementation) : base(name, 0)
        {
            this.implementation = implementation;
        }

        private readonly Func<bool> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(Name, 0, arglist, output);
            return implementation()
                   && k(output, env.Unifications, env.State, predecessor);
        }
    }

    /// <summary>
    /// A primitive task that implements a deterministic function from arguments to a boolean
    /// Requires all arguments to be instantiated
    /// </summary>
    public class SimplePredicate<T1> : PrimitiveTask
    {
        /// <summary>
        /// A primitive task that implements a deterministic function from arguments to a boolean
        /// Requires all arguments to be instantiated
        /// </summary>
        /// <param name="name">Name of the predicate</param>
        /// <param name="implementation">Low-level implementation of the predicate</param>
        public SimplePredicate(string name, Func<T1, bool> implementation) : base(name, 1)
        {
            this.implementation = implementation;
        }

        private readonly Func<T1, bool> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(Name, 1, arglist, output);
            return implementation(ArgumentTypeException.Cast<T1>(Name, env.Resolve(arglist[0]), arglist, output))
                   && k(output, env.Unifications, env.State, predecessor);
        }
    }

    /// <summary>
    /// A primitive task that implements a deterministic function from arguments to a boolean
    /// Requires all arguments to be instantiated
    /// </summary>
    public class SimplePredicate<T1, T2> : PrimitiveTask
    {
        /// <summary>
        /// A primitive task that implements a deterministic function from arguments to a boolean
        /// Requires all arguments to be instantiated
        /// </summary>
        /// <param name="name">Name of the predicate</param>
        /// <param name="implementation">Low-level implementation of the predicate</param>
        public SimplePredicate(string name, Func<T1, T2, bool> implementation) : base(name, 2)
        {
            this.implementation = implementation;
        }

        private readonly Func<T1, T2, bool> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(Name, 2, arglist, output);
            return implementation(
                       ArgumentTypeException.Cast<T1>(Name, env.Resolve(arglist[0]), arglist, output),
                       ArgumentTypeException.Cast<T2>(Name, env.Resolve(arglist[1]), arglist, output))
                   && k(output, env.Unifications, env.State, predecessor);
        }
    }

    /// <summary>
    /// A primitive task that implements a deterministic function from arguments to a boolean
    /// Requires all arguments to be instantiated
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class SimplePredicate<T1, T2, T3> : PrimitiveTask
    {
        /// <summary>
        /// A primitive task that implements a deterministic function from arguments to a boolean
        /// Requires all arguments to be instantiated
        /// </summary>
        /// <param name="name">Name of the predicate</param>
        /// <param name="implementation">Low-level implementation of the predicate</param>
        public SimplePredicate(string name, Func<T1, T2, T3, bool> implementation) : base(name, 3)
        {
            this.implementation = implementation;
        }

        private readonly Func<T1, T2, T3, bool> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(Name, 3, arglist, output);
            return implementation(
                       ArgumentTypeException.Cast<T1>(Name, env.Resolve(arglist[0]), arglist, output),
                       ArgumentTypeException.Cast<T2>(Name, env.Resolve(arglist[1]), arglist, output),
                       ArgumentTypeException.Cast<T3>(Name, env.Resolve(arglist[2]), arglist, output))
                   && k(output, env.Unifications, env.State, predecessor);
        }
    }

    /// <summary>
    /// A primitive task that implements a deterministic function from arguments to a boolean
    /// Requires all arguments to be instantiated
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class SimplePredicate<T1, T2, T3, T4> : PrimitiveTask
    {
        /// <summary>
        /// A primitive task that implements a deterministic function from arguments to a boolean
        /// Requires all arguments to be instantiated
        /// </summary>
        /// <param name="name">Name of the predicate</param>
        /// <param name="implementation">Low-level implementation of the predicate</param>
        public SimplePredicate(string name, Func<T1, T2, T3, T4, bool> implementation) : base(name, 4)
        {
            this.implementation = implementation;
        }

        private readonly Func<T1, T2, T3, T4, bool> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(Name, 4, arglist, output);
            return implementation(
                       ArgumentTypeException.Cast<T1>(Name, env.Resolve(arglist[0]), arglist, output),
                       ArgumentTypeException.Cast<T2>(Name, env.Resolve(arglist[1]), arglist, output),
                       ArgumentTypeException.Cast<T3>(Name, env.Resolve(arglist[2]), arglist, output),
                       ArgumentTypeException.Cast<T4>(Name, env.Resolve(arglist[3]), arglist, output))
                   && k(output, env.Unifications, env.State, predecessor);
        }
    }

    /// <summary>
    /// A primitive task that implements a deterministic function from arguments to a boolean
    /// Requires all arguments to be instantiated
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class SimplePredicate<T1, T2, T3, T4, T5> : PrimitiveTask
    {
        /// <summary>
        /// A primitive task that implements a deterministic function from arguments to a boolean
        /// Requires all arguments to be instantiated
        /// </summary>
        /// <param name="name">Name of the predicate</param>
        /// <param name="implementation">Low-level implementation of the predicate</param>
        public SimplePredicate(string name, Func<T1, T2, T3, T4, T5, bool> implementation) : base(name, 5)
        {
            this.implementation = implementation;
        }

        private readonly Func<T1, T2, T3, T4, T5, bool> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(Name, 5, arglist, output);
            return implementation(
                       ArgumentTypeException.Cast<T1>(Name, env.Resolve(arglist[0]), arglist, output),
                       ArgumentTypeException.Cast<T2>(Name, env.Resolve(arglist[1]), arglist, output),
                       ArgumentTypeException.Cast<T3>(Name, env.Resolve(arglist[2]), arglist, output),
                       ArgumentTypeException.Cast<T4>(Name, env.Resolve(arglist[3]), arglist, output),
                       ArgumentTypeException.Cast<T5>(Name, env.Resolve(arglist[4]), arglist, output))
                   && k(output, env.Unifications, env.State, predecessor);
        }
    }

    /// <summary>
    /// A primitive task that implements a deterministic function from arguments to a boolean
    /// Requires all arguments to be instantiated
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class SimplePredicate<T1, T2, T3, T4, T5, T6> : PrimitiveTask
    {
        /// <summary>
        /// A primitive task that implements a deterministic function from arguments to a boolean
        /// Requires all arguments to be instantiated
        /// </summary>
        /// <param name="name">Name of the predicate</param>
        /// <param name="implementation">Low-level implementation of the predicate</param>
        public SimplePredicate(string name, Func<T1, T2, T3, T4, T5, T6, bool> implementation) : base(name, 5)
        {
            this.implementation = implementation;
        }

        private readonly Func<T1, T2, T3, T4, T5, T6, bool> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(Name, 6, arglist, output);
            return implementation(
                       ArgumentTypeException.Cast<T1>(Name, env.Resolve(arglist[0]), arglist, output),
                       ArgumentTypeException.Cast<T2>(Name, env.Resolve(arglist[1]), arglist, output),
                       ArgumentTypeException.Cast<T3>(Name, env.Resolve(arglist[2]), arglist, output),
                       ArgumentTypeException.Cast<T4>(Name, env.Resolve(arglist[3]), arglist, output),
                       ArgumentTypeException.Cast<T5>(Name, env.Resolve(arglist[4]), arglist, output),
                       ArgumentTypeException.Cast<T6>(Name, env.Resolve(arglist[5]), arglist, output))
                   && k(output, env.Unifications, env.State, predecessor);
        }
    }

    /// <summary>
    /// A primitive task that implements a deterministic function from arguments to a boolean
    /// Requires all arguments to be instantiated
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class SimplePredicate<T1, T2, T3, T4, T5, T6, T7> : PrimitiveTask
    {
        /// <summary>
        /// A primitive task that implements a deterministic function from arguments to a boolean
        /// Requires all arguments to be instantiated
        /// </summary>
        /// <param name="name">Name of the predicate</param>
        /// <param name="implementation">Low-level implementation of the predicate</param>
        public SimplePredicate(string name, Func<T1, T2, T3, T4, T5, T6, T7, bool> implementation) : base(name, 5)
        {
            this.implementation = implementation;
        }

        private readonly Func<T1, T2, T3, T4, T5, T6, T7, bool> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(Name, 7, arglist, output);
            return implementation(
                       ArgumentTypeException.Cast<T1>(Name, env.Resolve(arglist[0]), arglist, output),
                       ArgumentTypeException.Cast<T2>(Name, env.Resolve(arglist[1]), arglist, output),
                       ArgumentTypeException.Cast<T3>(Name, env.Resolve(arglist[2]), arglist, output),
                       ArgumentTypeException.Cast<T4>(Name, env.Resolve(arglist[3]), arglist, output),
                       ArgumentTypeException.Cast<T5>(Name, env.Resolve(arglist[4]), arglist, output),
                       ArgumentTypeException.Cast<T6>(Name, env.Resolve(arglist[5]), arglist, output),
                       ArgumentTypeException.Cast<T7>(Name, env.Resolve(arglist[6]), arglist, output))
                   && k(output, env.Unifications, env.State, predecessor);
        }
    }

    /// <summary>
    /// A primitive task that implements a deterministic function from arguments to a boolean
    /// Requires all arguments to be instantiated
    /// </summary>
    public class SimpleNAryPredicate : PrimitiveTask
    {
        /// <summary>
        /// A primitive task that implements a deterministic function from arguments to a boolean
        /// Requires all arguments to be instantiated
        /// </summary>
        /// <param name="name">Name of the predicate</param>
        /// <param name="implementation">Low-level implementation of the predicate</param>
        public SimpleNAryPredicate(string name, Func<object?[], TextBuffer, bool> implementation) : base(name, null)
        {
            this.implementation = implementation;
        }

        private readonly Func<object?[], TextBuffer, bool> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
            => implementation(env.ResolveList(arglist), output) && k(output, env.Unifications, env.State, predecessor);
    }
}
