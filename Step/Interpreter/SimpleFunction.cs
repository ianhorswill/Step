using System;
using System.Collections.Generic;

namespace Step.Interpreter
{
    /// <summary>
    /// A predicate that represents a non-invertible function.
    /// </summary>
    /// <typeparam name="TIn">Type of function argument</typeparam>
    /// <typeparam name="TOut">Type of function value</typeparam>
    public class SimpleFunction<TIn, TOut> : GeneralPredicateBase
    {
        /// <summary>
        /// A predicate that represents a non-invertible function.
        /// </summary>
        /// <param name="name">Name of the function predicate</param>
        /// <param name="implementation">C# implementation of the function</param>
        public SimpleFunction(string name, Func<TIn, TOut> implementation) : base(name, 2)
        {
            this.implementation = implementation;
        }

        private readonly Func<TIn, TOut> implementation;

        /// <inheritdoc />
        protected override IEnumerable<BindingList<LogicVariable>> Iterator(object[] args, BindingEnvironment env)
        {
            ArgumentCountException.Check(Name, 2, args);
            var input = ArgumentTypeException.Cast<TIn>(Name, env.Resolve(args[0]), args);
            var result = implementation(input);
            if (env.Unify(args[1], result, out var bindings))
                return new[] { bindings };
            return EmptyBindingListArray;
        }
    }

    /// <summary>
    /// A predicate that represents a non-invertible function.
    /// </summary>
    /// <typeparam name="TIn1">Type of first argument</typeparam>
    /// <typeparam name="TIn2">Type of second argument</typeparam>
    /// <typeparam name="TOut">Type of function value</typeparam>
    public class SimpleFunction<TIn1, TIn2, TOut> : GeneralPredicateBase
    {
        /// <summary>
        /// A predicate that represents a non-invertible function.
        /// </summary>
        /// <param name="name">Name of the function predicate</param>
        /// <param name="implementation">C# implementation of the function</param>
        public SimpleFunction(string name, Func<TIn1, TIn2, TOut> implementation) : base(name, 3)
        {
            this.implementation = implementation;
        }

        private readonly Func<TIn1, TIn2, TOut> implementation;

        /// <inheritdoc />
        protected override IEnumerable<BindingList<LogicVariable>> Iterator(object[] args, BindingEnvironment env)
        {
            ArgumentCountException.Check(Name, 3, args);
            var input1 = ArgumentTypeException.Cast<TIn1>(Name, env.Resolve(args[0]), args);
            var input2 = ArgumentTypeException.Cast<TIn2>(Name, env.Resolve(args[1]), args);
            var result = implementation(input1, input2);
            if (env.Unify(args[2], result, out var bindings))
                return new[] { bindings };
            return EmptyBindingListArray;
        }
    }

    /// <summary>
    /// A predicate that represents a non-invertible function.
    /// </summary>
    /// <typeparam name="TIn1">Type of first argument</typeparam>
    /// <typeparam name="TIn2">Type of second argument</typeparam>
    /// <typeparam name="TIn3">Type of third argument</typeparam>
    /// <typeparam name="TOut">Type of function value</typeparam>
    // ReSharper disable once UnusedMember.Global
    public class SimpleFunction<TIn1, TIn2, TIn3, TOut> : GeneralPredicateBase
    {
        /// <summary>
        /// A predicate that represents a non-invertible function.
        /// </summary>
        /// <param name="name">Name of the function predicate</param>
        /// <param name="implementation">C# implementation of the function</param>
        public SimpleFunction(string name, Func<TIn1, TIn2, TIn3, TOut> implementation) : base(name, 4)
        {
            this.implementation = implementation;
        }

        private readonly Func<TIn1, TIn2, TIn3, TOut> implementation;

        /// <inheritdoc />
        protected override IEnumerable<BindingList<LogicVariable>> Iterator(object[] args, BindingEnvironment env)
        {
            ArgumentCountException.Check(Name, 4, args);
            var input1 = ArgumentTypeException.Cast<TIn1>(Name, args[0], args);
            var input2 = ArgumentTypeException.Cast<TIn2>(Name, args[1], args);
            var input3 = ArgumentTypeException.Cast<TIn3>(Name, args[2], args);
            var result = implementation(input1, input2, input3);
            if (env.Unify(args[3], result, out var bindings))
                return new[] { bindings };
            return EmptyBindingListArray;
        }
    }

    /// <summary>
    /// A predicate that represents a non-invertible function.
    /// </summary>
    /// <typeparam name="TIn1">Type of first argument</typeparam>
    /// <typeparam name="TIn2">Type of second argument</typeparam>
    /// <typeparam name="TIn3">Type of third argument</typeparam>
    /// <typeparam name="TIn4">Type of fourth argument</typeparam>
    /// <typeparam name="TOut">Type of function value</typeparam>
    // ReSharper disable once UnusedMember.Global
    public class SimpleFunction<TIn1, TIn2, TIn3, TIn4, TOut> : GeneralPredicateBase
    {
        /// <summary>
        /// A predicate that represents a non-invertible function.
        /// </summary>
        /// <param name="name">Name of the function predicate</param>
        /// <param name="implementation">C# implementation of the function</param>
        public SimpleFunction(string name, Func<TIn1, TIn2, TIn3, TIn4, TOut> implementation) : base(name, 5)
        {
            this.implementation = implementation;
        }

        private readonly Func<TIn1, TIn2, TIn3, TIn4, TOut> implementation;

        /// <inheritdoc />
        protected override IEnumerable<BindingList<LogicVariable>> Iterator(object[] args, BindingEnvironment env)
        {
            ArgumentCountException.Check(Name, 5, args);
            var input1 = ArgumentTypeException.Cast<TIn1>(Name, args[0], args);
            var input2 = ArgumentTypeException.Cast<TIn2>(Name, args[1], args);
            var input3 = ArgumentTypeException.Cast<TIn3>(Name, args[2], args);
            var input4 = ArgumentTypeException.Cast<TIn4>(Name, args[3], args);
            var result = implementation(input1, input2, input3, input4);
            if (env.Unify(args[4], result, out var bindings))
                return new[] { bindings };
            return EmptyBindingListArray;
        }
    }

    /// <summary>
    /// A predicate that represents a non-invertible function.
    /// </summary>
    /// <typeparam name="TIn1">Type of first argument</typeparam>
    /// <typeparam name="TIn2">Type of second argument</typeparam>
    /// <typeparam name="TIn3">Type of third argument</typeparam>
    /// <typeparam name="TIn4">Type of fourth argument</typeparam>
    /// <typeparam name="TIn5">Type of fifth argument</typeparam>
    /// <typeparam name="TOut">Type of function value</typeparam>
    // ReSharper disable once UnusedMember.Global
    public class SimpleFunction<TIn1, TIn2, TIn3, TIn4, TIn5, TOut> : GeneralPredicateBase
    {
        /// <summary>
        /// A predicate that represents a non-invertible function.
        /// </summary>
        /// <param name="name">Name of the function predicate</param>
        /// <param name="implementation">C# implementation of the function</param>
        public SimpleFunction(string name, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TOut> implementation) : base(name, 6)
        {
            this.implementation = implementation;
        }

        private readonly Func<TIn1, TIn2, TIn3, TIn4, TIn5, TOut> implementation;

        /// <inheritdoc />
        protected override IEnumerable<BindingList<LogicVariable>> Iterator(object[] args, BindingEnvironment env)
        {
            ArgumentCountException.Check(Name, 6, args);
            var input1 = ArgumentTypeException.Cast<TIn1>(Name, args[0], args);
            var input2 = ArgumentTypeException.Cast<TIn2>(Name, args[1], args);
            var input3 = ArgumentTypeException.Cast<TIn3>(Name, args[2], args);
            var input4 = ArgumentTypeException.Cast<TIn4>(Name, args[3], args);
            var input5 = ArgumentTypeException.Cast<TIn5>(Name, args[4], args);
            var result = implementation(input1, input2, input3, input4, input5);
            if (env.Unify(args[5], result, out var bindings))
                return new[] { bindings };
            return EmptyBindingListArray;
        }
    }

    /// <summary>
    /// A predicate that represents a non-invertible function.
    /// </summary>
    /// <typeparam name="TIn1">Type of first argument</typeparam>
    /// <typeparam name="TIn2">Type of second argument</typeparam>
    /// <typeparam name="TIn3">Type of third argument</typeparam>
    /// <typeparam name="TIn4">Type of fourth argument</typeparam>
    /// <typeparam name="TIn5">Type of fifth argument</typeparam>
    /// <typeparam name="TIn6">Type of sixth argument</typeparam>
    /// <typeparam name="TOut">Type of function value</typeparam>
    // ReSharper disable once UnusedMember.Global
    public class SimpleFunction<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut> : GeneralPredicateBase
    {
        /// <summary>
        /// A predicate that represents a non-invertible function.
        /// </summary>
        /// <param name="name">Name of the function predicate</param>
        /// <param name="implementation">C# implementation of the function</param>
        public SimpleFunction(string name, Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut> implementation) : base(name, 7)
        {
            this.implementation = implementation;
        }

        private readonly Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut> implementation;

        /// <inheritdoc />
        protected override IEnumerable<BindingList<LogicVariable>> Iterator(object[] args, BindingEnvironment env)
        {
            ArgumentCountException.Check(Name, 7, args);
            var input1 = ArgumentTypeException.Cast<TIn1>(Name, args[0], args);
            var input2 = ArgumentTypeException.Cast<TIn2>(Name, args[1], args);
            var input3 = ArgumentTypeException.Cast<TIn3>(Name, args[2], args);
            var input4 = ArgumentTypeException.Cast<TIn4>(Name, args[3], args);
            var input5 = ArgumentTypeException.Cast<TIn5>(Name, args[4], args);
            var input6 = ArgumentTypeException.Cast<TIn6>(Name, args[5], args);
            var result = implementation(input1, input2, input3, input4, input5, input6);
            if (env.Unify(args[6], result, out var bindings))
                return new[] { bindings };
            return EmptyBindingListArray;
        }
    }

    /// <summary>
    /// A simple function supporting a variable number of arguments
    /// All type checking is left to the implementation function
    /// </summary>
    public class SimpleNAryFunction : GeneralPredicateBase
    {
        /// <inheritdoc />
        public SimpleNAryFunction(string name, Func<object[], object> implementation) : base(name, null)
        {
            this.implementation = implementation;
        }
        private readonly Func<object[], object> implementation;

        /// <inheritdoc />
        protected override IEnumerable<BindingList<LogicVariable>> Iterator(object[] args, BindingEnvironment env)
        {
            ArgumentCountException.CheckAtLeast(Name, 1, args);
            var fArgs = new object[args.Length - 1];
            Array.Copy(args, fArgs, args.Length-1);
            var result = implementation(fArgs);
            if (env.Unify(args[args.Length-1], result, out var bindings))
                return new[] { bindings };
            return EmptyBindingListArray;
        }
    }
}
