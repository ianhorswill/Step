using System;
using System.Collections.Generic;

namespace Step.Interpreter
{
    /// <summary>
    /// A primitive task that generates text and succeeds a variable number of times (possibly not at all)
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class NondeterministicTextGenerator : PrimitiveTask
    {
        /// <summary>
        /// A primitive task that generates text and succeeds a variable number of times (possibly not at all)
        /// </summary>
        /// <param name="name">Name of the primitive</param>
        /// <param name="implementation">C# code to implement the primitive</param>
        public NondeterministicTextGenerator(string name, Func<IEnumerable<IEnumerable<string>>> implementation) : base(name, 0)
        {
            this.implementation = implementation;
        }

        private readonly Func<IEnumerable<IEnumerable<string>>> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(Name, 0, arglist);
            foreach (var tokens in implementation())
                if (k(output.Append(tokens), env.Unifications, env.State, predecessor))
                    return true;
            return false;
        }
    }

    /// <summary>
    /// A primitive task that generates text and succeeds a variable number of times (possibly not at all)
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class NondeterministicTextGenerator<T1> : PrimitiveTask
    {
        /// <summary>
        /// A primitive task that generates text and succeeds a variable number of times (possibly not at all)
        /// </summary>
        /// <param name="name">Name of the primitive</param>
        /// <param name="implementation">C# code to implement the primitive</param>
        public NondeterministicTextGenerator(string name, Func<T1, IEnumerable<IEnumerable<string>>> implementation) : base(name, 1)
        {
            this.implementation = implementation;
        }

        private readonly Func<T1, IEnumerable<IEnumerable<string>>> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(Name, 1, arglist);
            foreach (var tokens in implementation(ArgumentTypeException.Cast<T1>(Name, env.Resolve(arglist[0]), arglist)))
                if (k(output.Append(tokens), env.Unifications, env.State, predecessor))
                    return true;
            return false;
        }
    }

    /// <summary>
    /// A primitive task that generates text and succeeds a variable number of times (possibly not at all)
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class NondeterministicTextGenerator<T1, T2> : PrimitiveTask
    {
        /// <summary>
        /// A primitive task that generates text and succeeds a variable number of times (possibly not at all)
        /// </summary>
        /// <param name="name">Name of the primitive</param>
        /// <param name="implementation">C# code to implement the primitive</param>
        public NondeterministicTextGenerator(string name, Func<T1, T2, IEnumerable<IEnumerable<string>>> implementation) : base(name, 2)
        {
            this.implementation = implementation;
        }

        private readonly Func<T1, T2, IEnumerable<IEnumerable<string>>> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check(Name, 1, arglist);
            foreach (var tokens in implementation(
                ArgumentTypeException.Cast<T1>(Name, env.Resolve(arglist[0]), arglist),
                ArgumentTypeException.Cast<T2>(Name, env.Resolve(arglist[1]), arglist)))
                if (k(output.Append(tokens), env.Unifications, env.State, predecessor))
                    return true;
            return false;
        }
    }
}
