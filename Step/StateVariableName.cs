#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StateVariableName.cs" company="Ian Horswill">
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

using System.Collections.Generic;
using System.Diagnostics;
using Step.Serialization;
using Step.Interpreter;

namespace Step
{
    /// <summary>
    /// An object representing the name of a variable held in a Module
    /// This does not store the value itself; it's just a key to the
    /// tables in the Modules.  So it's like a symbol in lisp.
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class StateVariableName : StateElement, IVariableName, ISerializable
    {
        static StateVariableName()
        {
            Deserializer.RegisterHandler(typeof(StateVariableName), Deserialize);
        }

        /// <summary>
        /// Table mapping names to existing global variables
        /// </summary>
        private static readonly Dictionary<string,StateVariableName> SymbolTable = new Dictionary<string, StateVariableName>();

        private StateVariableName(string name) : base(name)
        { }

        /// <summary>
        /// Return the unique global variable with this name.
        /// Creates and stores the variable if necessary.
        /// </summary>
        /// <param name="name">Name for the variable</param>
        public static StateVariableName Named(string name)
        {
            if (name[0] == '^')
                name = name.Substring(1);
            if (SymbolTable.TryGetValue(name, out var global))
                return global;
            return SymbolTable[name] = new StateVariableName(name);
        }

        public void Serialize(Serializer s) => s.Write(Name);

        private static object Deserialize(Deserializer d) => Named(d.ReadAlphabetic());
    }
}
