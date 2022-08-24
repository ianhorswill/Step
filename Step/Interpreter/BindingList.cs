#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BindingList.cs" company="Ian Horswill">
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

namespace Step.Interpreter
{
    /// <summary>
    /// Represents values of variables of different types.
    /// In the case of LocalVariables, which can be unified, this might be another variable,
    /// in which case the bound variable has whatever value the other variable has.
    /// </summary>
    public class BindingList
    {
        /// <summary>
        /// Variable given a value by this cell of the linked list
        /// </summary>
        public readonly LogicVariable Variable;
        /// <summary>
        /// Value given to the Variable
        /// </summary>
        public readonly object? Value;
        /// <summary>
        /// Next cell in the binding list
        /// </summary>
        public readonly BindingList? Next;

        internal BindingList(LogicVariable variable, object? value, BindingList? next)
        {
            Variable = variable;
            Value = value;
            Next = next;
        }

        /// <summary>
        /// Attempt to find the value of the variable in the binding list
        /// </summary>
        /// <param name="bindingList">BindingList to check</param>
        /// <param name="variable">Variable to look for</param>
        /// <param name="value">Value, if found, or null</param>
        /// <returns>True if a value was found, otherwise false.</returns>
        // ReSharper disable once UnusedMember.Global
        public static bool TryLookup(BindingList? bindingList, LogicVariable variable, out object? value)
        {
            for (var cell = bindingList; cell != null; cell = cell.Next)
                if (ReferenceEquals(cell.Variable, variable))
                {
                    value = cell.Value;
                    return true;
                }

            value = null;
            return false;
        }

        /// <summary>
        /// Return value of the variable in the (possibly empty) binding list
        /// </summary>
        /// <param name="bindingList">List to check</param>
        /// <param name="v">Variable to look up</param>
        /// <param name="defaultValue">Default value to return if the variable isn't found</param>
        /// <returns>Value of variable or defaultValue if not found</returns>
        public static object? Lookup(BindingList? bindingList, LogicVariable v, object defaultValue)
            => bindingList == null ? defaultValue : bindingList.Lookup(v, defaultValue);

        /// <summary>
        /// Find the value to which the variable is bound
        /// </summary>
        /// <param name="v">Variable whose value to look up</param>
        /// <param name="defaultValue">Value to return if the variable isn't bound in this binding list</param>
        /// <returns>Value of the variable or defaultValue</returns>
        public object? Lookup(LogicVariable v, object defaultValue)
        {
            for (var cell = this; cell != null; cell = cell.Next)
                if (ReferenceEquals(cell.Variable, v))
                    return cell.Value;
            return defaultValue;
        }
        
        /// <summary>
        /// Make a new binding list with specified additional binding
        /// USE STATIC VERSION IF ORIGINAL LIST MIGHT BE NULL
        /// </summary>
        public BindingList Bind(LogicVariable variable, object? value)
            => new BindingList(variable, value, this);

        /// <summary>
        /// Make a new binding list with specified additional binding
        /// </summary>
        public static BindingList Bind(BindingList? bindings, LogicVariable variable, object? value)
            => new BindingList(variable, value, bindings);
    }
}