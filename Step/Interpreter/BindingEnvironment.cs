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
using System.Linq;

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

        /// <summary>
        /// MethodCallFrame of this environment
        /// </summary>
        public readonly MethodCallFrame Frame;
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
        public readonly State State;

        /// <summary>
        /// Make a new binding environment based on the specified environment, with the specified change(s)
        /// </summary>
        internal BindingEnvironment(BindingEnvironment e, MethodCallFrame frame)
            : this(e.Module, frame, e.Unifications, e.State)
        { }

        /// <summary>
        /// Make a new binding environment based on the specified environment, with the specified change(s)
        /// </summary>
        internal BindingEnvironment(Module module, MethodCallFrame frame)
            : this(module, frame, null, State.Empty)
        { }

        /// <summary>
        /// Make a new binding environment based on the specified environment, with the specified change(s)
        /// </summary>
        public BindingEnvironment(BindingEnvironment e, BindingList<LogicVariable> unifications, State state)
            : this(e.Module, e.Frame, unifications, state)
        { }

        /// <summary>
        /// Make a binding environment identical to e but with v bound to newValue
        /// </summary>
        public BindingEnvironment(BindingEnvironment e, StateElement v, object newValue)
            : this(e.Module, e.Frame, e.Unifications, e.State.Bind(v, newValue))
        { }

        /// <summary>
        /// Make a new binding environment with the specified components
        /// </summary>
        public BindingEnvironment(Module module, MethodCallFrame frame,
            BindingList<LogicVariable> unifications, State state)
        {
            Module = module;
            Frame = frame;
            Unifications = unifications;
            State = state;
        }

        /// <summary>
        /// Make a new binding environment with nothing in it.
        /// Used for Unit tests.  Don't use this yourself.
        /// </summary>
        internal static BindingEnvironment NewEmpty() =>
            new BindingEnvironment(new Module("empty"), 
                new MethodCallFrame(null, null, new LogicVariable[0], null, null));

        /// <summary>
        /// Canonicalize a term, i.e. get its value, or reduce it to a logic variable
        /// if it doesn't have a value yet
        /// </summary>
        public object Resolve(object term, BindingList<LogicVariable> unifications)
        {
            switch (term)
            {
                case LocalVariableName l:
                    term = Deref(Local[l.Index], unifications);
                    break;

                case StateVariableName g:
                    term = State.TryGetValue(g, out var result) ? result : Module[g];
                    break;

                case LogicVariable v:
                    term = Deref(v, unifications);
                    break;
            }

            switch (term)
            {
                case null:
                    return null;

                case string[] tokens:
                    return tokens;

                case object[] sublist:
                    return ResolveList(sublist, unifications);

                default:
                    return term;
            }
        }

        /// <summary>
        /// Canonicalize a list of terms, i.e. get their values or reduce them to (unbound) logic variables.
        /// </summary>
        public object Resolve(object term) => Resolve(term, Unifications);

        /// <summary>
        /// Canonicalize a list of terms, i.e. get their values or reduce them to (unbound) logic variables.
        /// </summary>
        public object[] ResolveList(object[] arglist, BindingList<LogicVariable> unifications)
        {
            var result = new object[arglist.Length];
            for (var i = 0; i < arglist.Length; i++)
                result[i] = Resolve(arglist[i], unifications);
            return result;
        }

        /// <summary>
        /// Canonicalize a list of terms, i.e. get their values or reduce them to (unbound) logic variables.
        /// </summary>
        public object[] ResolveList(object[] arglist) => ResolveList(arglist, Unifications);

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
            a = Resolve(a, inUnifications);
            b = Resolve(b, inUnifications);
            if (a == null || b == null)
            {
                if (a is LogicVariable av)
                {
                    outUnifications = new BindingList<LogicVariable>(av, null, inUnifications);
                    return true;
                }
                else if (b is LogicVariable bv)
                {
                    outUnifications = new BindingList<LogicVariable>(bv, null, inUnifications);
                    return true;
                }

                outUnifications = inUnifications;
                return ReferenceEquals(a, b);
            }
            if (a.Equals(b))
            {
                outUnifications = inUnifications;
                return true;
            }
            if (a is LogicVariable va)
            {
                if (b is LogicVariable vbb && va.Uid < vbb.Uid)
                    // a is older than b, so make b point to a
                    outUnifications = new BindingList<LogicVariable>(vbb, va, inUnifications);
                else
                    outUnifications = new BindingList<LogicVariable>(va, b, inUnifications);
                return true;
            }

            if (b is LogicVariable vb)
            {
                outUnifications = new BindingList<LogicVariable>(vb, a, inUnifications);
                return true;
            }

            if (a is object[] aa && b is object[] ba && aa.Length == ba.Length)
                return UnifyArrays(aa, ba, inUnifications, out outUnifications);

            outUnifications = inUnifications;
            return false;
        }
        
        /// <summary>
        /// Attempt to unify two terms
        /// </summary>
        /// <param name="a">First term</param>
        /// <param name="b">Other term</param>
        /// <param name="outUnifications">Substitutions in place after unification, if unification successful</param>
        /// <returns>True if the objects are unifiable and outUnification holds their most general unifier</returns>
        public bool Unify(object a, object b, out BindingList<LogicVariable> outUnifications)
            => Unify(a, b, Unifications, out outUnifications);
        
        private bool UnifyArrays(object[] a, object[] b, BindingList<LogicVariable> inUnifications, out BindingList<LogicVariable> outUnifications)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Argument arrays to UnifyArrays are of different lengths!");
            outUnifications = inUnifications;
            for (var i = 0; i < a.Length; i++)
                if (!Unify(a[i], b[i], outUnifications, out outUnifications))
                    return false;
            return true;
        }

        /// <summary>
        /// Unifies the elements of two arrays using this environment's binding list
        /// </summary>
        /// <param name="a">First array</param>
        /// <param name="b">Second array</param>
        /// <param name="outUnifications">Extended binding list</param>
        /// <returns>True if successful</returns>
        public bool UnifyArrays(object[] a, object[] b, out BindingList<LogicVariable> outUnifications)
            => UnifyArrays(a, b, Unifications, out outUnifications);

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
                e = new BindingEnvironment(Module, Frame, outUnifications, State);
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
        private object Deref(object value) => Deref(value, Unifications);

        /// <summary>
        /// If value is a LogicVariable, follow the chain of substitutions in Unifications to reduce it to its normal form.
        /// If it's not a logic variable, just returns the value.
        /// </summary>
        public static object Deref(object value, BindingList<LogicVariable> unifications)
        {
            while (value is LogicVariable v)
            {
                value = BindingList<LogicVariable>.Lookup(unifications, v, v);
                if (value == v)
                    // Isn't aliased to a value
                    return v;
            }

            return value;
        }
        
        /// <summary>
        /// Dereference all variables in term
        /// This behaves identically to Deref except in the case where term is a tuple (i.e. object[]), in which case it
        /// recursively recopies the array and dereferences its elements
        /// </summary>
        public object CopyTerm(object term)
        {
            switch (term)
            {
                case LogicVariable l:
                    var d = Deref(l);
                    return d is LogicVariable ? d : CopyTerm(d);

                case object[] tuple:
                    return tuple.Select(CopyTerm).ToArray();

                default:
                    return term;
            }
        }

        /// <summary>
        /// Dereference all variables in term
        /// If the resulting term has uninstantiated variables, return false.
        /// </summary>
        public bool TryCopyGround(object term, out object copied)
        {
            switch (term)
            {
                case LogicVariable l:
                    copied = Deref(l);
                    if (copied is LogicVariable)
                        return false;
                    return TryCopyGround(copied, out copied);

                case LocalVariableName l:
                    return TryCopyGround(Local[l.Index], out copied);

                case string[] tokens:
                    copied = tokens;
                    return true;
                
                case object[] tuple:
                    var newTuple = new object[tuple.Length];
                    copied = newTuple;
                    for (var i = 0; i < newTuple.Length; i++)
                        if (!TryCopyGround(tuple[i], out newTuple[i]))
                            return false;
                    return true;

                default:
                    copied = term;
                    return true;
            }
        }
    }
}
