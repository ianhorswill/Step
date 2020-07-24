#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BindingEnvironment.cs" company="Ian Horswill">
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

namespace Step.Interpreter
{
    /// <summary>
    /// Represents all information about variable binding at a particular point in execution
    /// Binding environments are readonly because we have to support backtracking: when we bind
    /// a variable, we make a new binding environment, so that the old environment still exists
    /// if we have to backtrack.
    /// </summary>
    public readonly struct BindingEnvironment
    {
        /// <summary>
        /// Module containing bindings of GlobalVariables
        /// </summary>
        public readonly Module Module;

        internal readonly MethodCallFrame Frame;
        /// <summary>
        /// Logic variables holding the values of the current method's local variables
        /// </summary>
        public LogicVariable[] Local => Frame.Locals;
        /// <summary>
        /// Bindings mapping local variables to their values, or to other local variables
        /// </summary>
        public readonly BindingList<LogicVariable> Unifications;
        /// <summary>
        /// Bindings mapping global variables to their values, when overriding the Module's values
        /// </summary>
        public readonly BindingList<GlobalVariableName> DynamicState;

        /// <summary>
        /// Make a new binding environment based on the specified environment, with the specified change(s)
        /// </summary>
        internal BindingEnvironment(BindingEnvironment e, MethodCallFrame frame)
            : this(e.Module, frame, e.Unifications, e.DynamicState)
        { }

        /// <summary>
        /// Make a new binding environment based on the specified environment, with the specified change(s)
        /// </summary>
        internal BindingEnvironment(Module module, MethodCallFrame frame)
            : this(module, frame, null, null)
        { }

        /// <summary>
        /// Make a new binding environment based on the specified environment, with the specified change(s)
        /// </summary>
        public BindingEnvironment(BindingEnvironment e, BindingList<LogicVariable> unifications, BindingList<GlobalVariableName> dynamicState)
            : this(e.Module, e.Frame, unifications, dynamicState)
        { }

        /// <summary>
        /// Make a binding environment identical to e but with v bound to newValue
        /// </summary>
        public BindingEnvironment(BindingEnvironment e, GlobalVariableName v, object newValue)
            : this(e.Module, e.Frame, e.Unifications, new BindingList<GlobalVariableName>(v, newValue, e.DynamicState))
        { }

        internal BindingEnvironment(Module module, MethodCallFrame frame,
            BindingList<LogicVariable> unifications, BindingList<GlobalVariableName> dynamicState)
        {
            Module = module;
            Frame = frame;
            Unifications = unifications;
            DynamicState = dynamicState;
        }

        /// <summary>
        /// Make a new binding environment with nothing in it.
        /// Used for Unit tests.  Don't use this yourself.
        /// </summary>
        internal static BindingEnvironment NewEmpty() =>
            new BindingEnvironment(new Module(), 
                new MethodCallFrame(null, null, new LogicVariable[0], null));

        /// <summary>
        /// Canonicalize a term, i.e. get its value, or reduce it to a logic variable
        /// if it doesn't have a value yet
        /// </summary>
        public object Resolve(object term)
        {
            switch (term)
            {
                case LocalVariableName l:
                    return Deref(Local[l.Index]);

                case GlobalVariableName g:
                    if (BindingList<GlobalVariableName>.TryLookup(DynamicState, g, out var result))
                        return result;
                    return Module[g];

                case string[] tokens:
                    return tokens;

                case object[] sublist:
                    return ResolveList(sublist);

                case LogicVariable v:
                    return Deref(v);

                default:
                    return term;
            }
        }

        /// <summary>
        /// Canonicalize a list of terms, i.e. get their values or reduce them to (unbound) logic variables.
        /// </summary>
        public object[] ResolveList(object[] arglist)
        {
            var result = new object[arglist.Length];
            for (var i = 0; i < arglist.Length; i++)
                result[i] = Resolve(arglist[i]);
            return result;
        }

        /// <summary>
        /// Attempt to unify two terms
        /// </summary>
        /// <param name="a">First term</param>
        /// <param name="b">Other term</param>
        /// <param name="inUnifications">Substitutions currently in place</param>
        /// <param name="outUnifications">Substitutions in place after unification, if unification successful</param>
        /// <returns>True if the objects are unifiable and outUnification holds their most general unifier</returns>
        public bool Unify(object a, object b, BindingList<LogicVariable> inUnifications, out BindingList<LogicVariable> outUnifications)
        {
            a = Resolve(a);
            b = Resolve(b);
            if (a is LogicVariable va)
            {
                outUnifications = new BindingList<LogicVariable>(va, b, inUnifications);
                return true;
            }

            if (b is LogicVariable vb)
            {
                outUnifications = new BindingList<LogicVariable>(vb, a, inUnifications);
                return true;
            }

            outUnifications = inUnifications;
            return a.Equals(b);
        }

        private bool UnifyArrays(object[] a, object[] b, out BindingList<LogicVariable> outUnifications)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Argument arrays to UnifyArrays are of different lengths!");
            outUnifications = Unifications;
            for (var i = 0; i < a.Length; i++)
                if (!Unify(a[i], b[i], outUnifications, out outUnifications))
                    return false;
            return true;
        }

        /// <summary>
        /// Attempt to unify two arrays of terms
        /// </summary>
        /// <param name="a">First array term</param>
        /// <param name="b">Other array term</param>
        /// <param name="e">Resulting BindingEnvironment.  This is the same as this BindingEnvironment, but possibly with a longer Unifications list.</param>
        /// <returns>True if the objects are unifiable and e holds their most general unifier</returns>
        public bool UnifyArrays(object[] a, object[] b, out BindingEnvironment e)
        {
            if (UnifyArrays(a, b, out BindingList<LogicVariable> outUnifications))
            {
                e = new BindingEnvironment(Module, Frame, outUnifications, DynamicState);
                return true;
            }

            e = this;
            return false;
        }

        /// <summary>
        /// If value is a LogicVariable, follow the chain of substitutions in Unifications to reduce it to its normal form.
        /// If it's not a logic variable, just returns the value.
        /// </summary>
        /// <param name="value">Term</param>
        /// <returns>Reduced value of term.  Could be a LogicVariable, in which case it reduces to an unbound variable.</returns>
        private object Deref(object value)
        {
            while (value is LogicVariable v)
            {
                value = BindingList<LogicVariable>.Lookup(Unifications, v, v);
                if (value == v)
                    // Isn't aliased to a value
                    return v;
            }

            return value;
        }
    }
}
