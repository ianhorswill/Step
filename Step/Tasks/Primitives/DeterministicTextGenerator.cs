using Step.Binding;
using Step.Exceptions;
using Step.Interpreter;
using Step.Output;
using System;
using System.Collections.Generic;

namespace Step.Tasks.Primitives
{
    /// <summary>
    /// A primitive that deterministically maps inputs to output text
    /// </summary>
    public class DeterministicTextGenerator : PrimitiveTask
    {
        /// <summary>
        /// A primitive that deterministically maps inputs to output text
        /// </summary>
        /// <param name="name">Name of primitive</param>
        /// <param name="implementation">Delegate to implement the mapping</param>
        public DeterministicTextGenerator(string name, Func<IEnumerable<string>> implementation) : base(name, 0)
        {
            this.implementation = implementation;
        }

        private readonly Func<IEnumerable<string>> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Continuation k)
        {
            ArgumentCountException.Check(Name, 0, arglist, output);
            return k(output.Append(implementation()), env.Unifications, env.State, predecessor);
        }
    }

    /// <summary>
    /// A primitive that deterministically maps inputs to output text
    /// </summary>
    public class DeterministicTextGenerator<T1> : PrimitiveTask
    {
        /// <summary>
        /// A primitive that deterministically maps inputs to output text
        /// </summary>
        /// <param name="name">Name of primitive</param>
        /// <param name="implementation">Delegate to implement the mapping</param>
        public DeterministicTextGenerator(string name, Func<T1, IEnumerable<string>> implementation) : base(name, 1)
        {
            this.implementation = implementation;
        }

        private readonly Func<T1, IEnumerable<string>> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Continuation k)
        {
            ArgumentCountException.Check(Name, 1, arglist, output);
            return k(output.Append(implementation(ArgumentTypeException.Cast<T1>(Name, env.Resolve(arglist[0]), arglist, output))),
                env.Unifications, env.State, predecessor);
        }
    }

    /// <summary>
    /// A primitive that deterministically maps inputs to output text
    /// </summary>
    public class DeterministicTextGenerator<T1, T2> : PrimitiveTask
    {
        /// <summary>
        /// A primitive that deterministically maps inputs to output text
        /// </summary>
        /// <param name="name">Name of primitive</param>
        /// <param name="implementation">Delegate to implement the mapping</param>
        public DeterministicTextGenerator(string name, Func<T1, T2, IEnumerable<string>> implementation) : base(name, 2)
        {
            this.implementation = implementation;
        }

        private readonly Func<T1, T2, IEnumerable<string>> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Continuation k)
        {
            ArgumentCountException.Check(Name, 2, arglist, output);
            return k(output.Append(implementation(
                                    ArgumentTypeException.Cast<T1>(Name, env.Resolve(arglist[0]), arglist, output),
                                    ArgumentTypeException.Cast<T2>(Name, env.Resolve(arglist[1]), arglist, output))),
                    env.Unifications, env.State, predecessor);
        }
    }

    /// <summary>
    /// A primitive that deterministically maps inputs to output text
    /// </summary>
    public class DeterministicTextGeneratorMetaTask : PrimitiveTask
    {
        /// <summary>
        /// A primitive that deterministically maps inputs to output text
        /// </summary>
        /// <param name="name">Name of primitive</param>
        /// <param name="implementation">Delegate to implement the mapping</param>
        public DeterministicTextGeneratorMetaTask(string name,
            Func<object?[], TextBuffer, BindingEnvironment, MethodCallFrame?, IEnumerable<string>> implementation) : base(name, null)
        {
            this.implementation = implementation;
        }

        private readonly Func<object?[], TextBuffer, BindingEnvironment, MethodCallFrame?, IEnumerable<string>> implementation;

        /// <inheritdoc />
        public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Continuation k)
        {
            var text = implementation(arglist, output, env, predecessor);
            return k(output.Append(text), env.Unifications, env.State, predecessor);
        }
    }
}
