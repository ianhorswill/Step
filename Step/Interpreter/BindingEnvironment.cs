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
    public readonly struct BindingEnvironment
    {
        public readonly Module Module;
        public readonly LogicVariable[] Local;
        public readonly BindingList<LogicVariable> Unifications;
        public readonly BindingList<GlobalVariable> DynamicState;

        public BindingEnvironment(BindingEnvironment e, LogicVariable[] local)
            : this(e.Module, local, e.Unifications, e.DynamicState)
        { }

        public BindingEnvironment(Module module, LogicVariable[] local)
            : this(module, local, null, null)
        { }

        public BindingEnvironment(BindingEnvironment e, BindingList<LogicVariable> unifications, BindingList<GlobalVariable> dynamicState)
            : this(e.Module, e.Local, unifications, dynamicState)
        { }

        public BindingEnvironment(BindingEnvironment e, GlobalVariable v, object newValue)
            : this(e.Module, e.Local, e.Unifications, new BindingList<GlobalVariable>(v, newValue, e.DynamicState))
        { }

        private BindingEnvironment(Module module, LogicVariable[] local,
            BindingList<LogicVariable> unifications, BindingList<GlobalVariable> dynamicState)
        {
            Module = module;
            Local = local;
            Unifications = unifications;
            DynamicState = dynamicState;
        }

        public static BindingEnvironment NewEmpty() => new BindingEnvironment(new Module(), new LogicVariable[0]);

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

                case GlobalVariable g:
                    if (BindingList<GlobalVariable>.TryLookup(DynamicState, g, out var result))
                        return result;
                    return Module[g];

                default:
                    return term;
            }
        }

        public object[] ResolveList(object[] arglist)
        {
            var result = new object[arglist.Length];
            for (var i = 0; i < arglist.Length; i++)
                result[i] = Resolve(arglist[i]);
            return result;
        }

        private bool Unify(object a, object b, BindingList<LogicVariable> inUnifications, out BindingList<LogicVariable> outUnifications)
        {
            a = Resolve(a);
            b = Resolve(b);
            if (a is LogicVariable va)
            {
                outUnifications = new BindingList<LogicVariable>(va, b, inUnifications);
                return true;
            } else if (b is LogicVariable vb)
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

        public bool Unify(object a, object b, out BindingEnvironment e)
        {
            if (Unify(a, b, Unifications, out BindingList<LogicVariable> outUnifications))
            {
                e = new BindingEnvironment(Module, Local, outUnifications, DynamicState);
                return true;
            }

            e = this;
            return false;
        }

        public bool UnifyArrays(object[] a, object[] b, out BindingEnvironment e)
        {
            if (UnifyArrays(a, b, out BindingList<LogicVariable> outUnifications))
            {
                e = new BindingEnvironment(Module, Local, outUnifications, DynamicState);
                return true;
            }

            e = this;
            return false;
        }

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
