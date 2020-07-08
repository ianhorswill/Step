#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrimitiveTask.cs" company="Ian Horswill">
// Copyright (C) 2020 Ian Horswill
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace Step.Interpreter
{
    /// <summary>
    /// Definitions used in making Tasks that are implemented directly as C# code.
    /// </summary>
    public static class PrimitiveTask
    {
        /// <summary>
        /// A primitive that just succeeds or fails, without generating output
        /// </summary>
        /// <param name="arg1">Argument to the predicate</param>
        /// <returns>Whether the predicate should succeed or fail</returns>
        public delegate bool Predicate1(object arg1);
        /// <summary>
        /// A primitive that just succeeds or fails, without generating output
        /// </summary>
        /// <param name="arg1">Argument to the predicate</param>
        /// <param name="arg2">Argument to the predicate</param>
        /// <returns>Whether the predicate should succeed or fail</returns>
        public delegate bool Predicate2(object arg1, object arg2);

        /// <summary>
        /// Wraps a C# predicate in type checking code.
        /// </summary>
        /// <typeparam name="T">Expected type of the predicate's argument</typeparam>
        /// <param name="name">Task name to give to the predicate</param>
        /// <param name="realFunction">Implementation as a C# delegate</param>
        /// <returns></returns>
        public static Predicate1 Predicate<T>(string name, Func<T, bool> realFunction)
        {
            return o =>
            {
                ArgumentTypeException.Check(name, typeof(T), o);
                return realFunction((T) o);
            };
        }
        /// <summary>
        /// Wraps a C# predicate in type checking code.
        /// </summary>
        /// <typeparam name="T1">Expected type of the predicate's first argument</typeparam>
        /// <typeparam name="T2">Expected type of the predicate's second argument</typeparam>
        /// <param name="name">Task name to give to the predicate</param>
        /// <param name="realFunction">Implementation as a C# delegate</param>
        /// <returns></returns>
        public static Predicate2 Predicate<T1,T2>(string name, Func<T1, T2, bool> realFunction)
        {
            return (o1, o2) =>
            {
                ArgumentTypeException.Check(name, typeof(T1), o1);
                ArgumentTypeException.Check(name, typeof(T2), o2);
                return realFunction((T1)o1, (T2)o2);
            };
        }

        public delegate bool MetaTask(object[] args, PartialOutput o, BindingEnvironment e, Step.Continuation k);

        /// <summary>
        /// A primitive task that generates text and always succeeds once (i.e. you can't backtrack to get different alternative versions of the text.
        /// </summary>
        /// <returns>Generated text</returns>
        public delegate IEnumerable<string> DeterministicTextGenerator0();
        /// <summary>
        /// A primitive task that generates text and always succeeds once (i.e. you can't backtrack to get different alternative versions of the text.
        /// </summary>
        /// <returns>Generated text</returns>
        public delegate IEnumerable<string> DeterministicTextGenerator1(object arg1);
        /// <summary>
        /// A primitive task that generates text and always succeeds once (i.e. you can't backtrack to get different alternative versions of the text.
        /// </summary>
        /// <returns>Generated text</returns>
        public delegate IEnumerable<string> DeterministicTextGenerator2(object arg1, object arg2);

        public delegate IEnumerable<string> DeterministicTextGeneratorMetaTask(object[] args, PartialOutput o, BindingEnvironment e);

        /// <summary>
        /// Wrap a C# procedure in type checking code and return it as a DeterministicTextGenerator1
        /// </summary>
        /// <param name="name">Name to give to the primitive method (for use in error reporting)</param>
        /// <param name="realFunction">Implementation</param>
        /// <typeparam name="T">Type of argument to task</typeparam>
        public static DeterministicTextGenerator1 DeterministicText<T>(string name, Func<T, IEnumerable<string>> realFunction)
        {
            return o =>
            {
                ArgumentTypeException.Check(name, typeof(T), o);
                return realFunction((T) o);
            };
        }

        /// <summary>
        /// Wrap a C# procedure in type checking code and return it as a DeterministicTextGenerator1
        /// </summary>
        /// <param name="name">Name to give to the primitive method (for use in error reporting)</param>
        /// <param name="realFunction">Implementation</param>
        /// <typeparam name="T1">Type of argument to task</typeparam>
        /// <typeparam name="T2">Type of second argument to task</typeparam>
        public static DeterministicTextGenerator2 DeterministicText<T1, T2>(string name, Func<T1, T2, IEnumerable<string>> realFunction)
        {
            return (o1, o2) =>
            {
                ArgumentTypeException.Check(name, typeof(T1), o1);
                ArgumentTypeException.Check(name, typeof(T2), o2);
                return realFunction((T1) o1, (T2)o2);
            };
        }

        /// <summary>
        /// A primitive task that generates text and succeeds a variable number of times (possibly not at all)
        /// </summary>
        /// <returns>Each element is a string enumeration for one possible success of this primitive.</returns>
        public delegate IEnumerable<IEnumerable<string>> NondeterministicTextGenerator0();
        /// <summary>
        /// A primitive task that generates text and succeeds a variable number of times (possibly not at all)
        /// </summary>
        /// <returns>Each element is a string enumeration for one possible success of this primitive.</returns>
        public delegate IEnumerable<IEnumerable<string>> NondeterministicTextGenerator1(object arg1);
        /// <summary>
        /// A primitive task that generates text and succeeds a variable number of times (possibly not at all)
        /// </summary>
        /// <returns>Each element is a string enumeration for one possible success of this primitive.</returns>
        public delegate IEnumerable<IEnumerable<string>> NondeterministicTextGenerator2(object arg1, object arg2);

        /// <summary>
        /// A relation that behaves like a full logic programming predicate, i.e. can run forwards and backward, succeed multiple times, etc.
        /// </summary>
        public delegate IEnumerable<BindingList<LogicVariable>> NonDeterministicRelation(object[] args, BindingEnvironment e);

        /// <summary>
        /// Make a general user-defined unary predicate
        /// </summary>
        /// <param name="name">Name of the predicate</param>
        /// <param name="inMode">Implementation when argument is value</param>
        /// <param name="outMode">Implementation when argument is an unbound variable</param>
        /// <typeparam name="T">Type of argument</typeparam>
        /// <returns>Implementation that can be called by internal code</returns>
        public static NonDeterministicRelation GeneralRelation<T>(string name, Func<T, bool> inMode,
            Func<IEnumerable<T>> outMode)
        {
            return (args, e) => UnaryPredicateTrampoline(name, inMode, outMode, args, e);
        }

        /// <summary>
        /// Make a general user-defined unary predicate
        /// </summary>
        /// <param name="name">Name of the predicate</param>
        /// <param name="inInMode">Implementation for when both arguments are bound</param>
        /// <param name="inOutMode">Implementation for when the first argument is bound and the second isn't</param>
        /// <param name="outInMode">Implementation for when the first argument is unbound and the second is</param>
        /// <param name="outOutMode">Implementation for when neither argument is bound</param>
        /// <typeparam name="T1">Type of the first argument</typeparam>
        /// <typeparam name="T2">Type of the second argument</typeparam>
        /// <returns>Implementation that can be called by internal code</returns>
        public static NonDeterministicRelation GeneralRelation<T1, T2>(string name, Func<T1, T2, bool> inInMode,
            Func<T1, IEnumerable<T2>> inOutMode, Func<T2, IEnumerable<T1>> outInMode,
            Func<IEnumerable<(T1, T2)>> outOutMode)
        {
            return (args, e) => BinaryPredicateTrampoline(name, inInMode, inOutMode, outInMode, outOutMode, args, e);
        }

        private static IEnumerable<BindingList<LogicVariable>> UnaryPredicateTrampoline<T>(string name, Func<T, bool> inMode,
            Func<IEnumerable<T>> outMode, object[] args, BindingEnvironment e)
        {
            ArgumentCountException.Check(name, 1, args);
            var arg = e.Resolve(args[0]);
            switch (arg)
            {
                case LogicVariable v:
                {
                    foreach (var result in outMode())
                        yield return e.Unifications.Bind(v, result);
                    break;
                }
                case T value:
                    if (inMode(value))
                        yield return e.Unifications;
                    break;
                default:
                    throw new ArgumentTypeException(name, typeof(T), arg);
            }
        }

        private static IEnumerable<BindingList<LogicVariable>> BinaryPredicateTrampoline<T1, T2>(string name,
            Func<T1, T2, bool> inInMode,
            Func<T1, IEnumerable<T2>> inOutMode, Func<T2, IEnumerable<T1>> outInMode,
            Func<IEnumerable<(T1, T2)>> outOutMode, object[] args, BindingEnvironment e)
        {
            ArgumentCountException.Check(name, 2, args);
            var arg1 = e.Resolve(args[0]);
            var arg2 = e.Resolve(args[1]);
            switch (arg1)
            {
                case LogicVariable v1:
                    switch (arg2)
                    {
                        case LogicVariable v2:
                            if (outOutMode == null)
                                throw new ArgumentInstantiationException(name, e, args);

                            foreach (var (out1, out2) in outOutMode())
                                yield return e.Unifications.Bind(v1, out1).Bind(v2, out2);
                            break;

                        case T2 in2:
                            if (outInMode == null)
                                throw new ArgumentInstantiationException(name, e, args);

                            foreach (var out1 in outInMode(in2))
                                yield return e.Unifications.Bind(v1, out1);
                            break;

                        default:
                            throw new ArgumentTypeException(name, typeof(T2), arg2);
                    }

                    break;

                case T1 in1:
                    switch (arg2)
                    {
                        case LogicVariable v2:
                            if (inOutMode == null)
                                throw new ArgumentInstantiationException(name, e, args);

                            foreach (var out2 in inOutMode(in1))
                                yield return e.Unifications.Bind(v2, out2);
                            break;

                        case T2 in2:
                            if (inInMode == null)
                                throw new ArgumentInstantiationException(name, e, args);

                            if (inInMode(in1, in2))
                                yield return e.Unifications;
                            break;

                        default:
                            throw new ArgumentTypeException(name, typeof(T2), arg2);
                    }

                    break;

                default:
                    throw new ArgumentTypeException(name, typeof(T1), arg1);
            }
        }
    }
}
