#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PartialOutput.cs" company="Ian Horswill">
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
using System.Diagnostics;

namespace Step.Interpreter
{
    [DebuggerDisplay("{" + nameof(AsString) + "}")]
    public readonly struct PartialOutput
    {
        public readonly string[] Buffer;
        public readonly int Length;

        private PartialOutput(string[] buffer, int length)
        {
            Buffer = buffer;
            Length = length;
        }

        private const int DefaultCapacity = 500;
        public PartialOutput(int capacity)
            : this(new string[capacity])
        { }

        public PartialOutput(string[] buffer) : this(buffer, 0) { }

        public static PartialOutput NewEmpty() => new PartialOutput(DefaultCapacity);

        public PartialOutput Append(string[] tokens)
        {
            Array.Copy(tokens, 0, Buffer, Length, tokens.Length);
            return new PartialOutput(Buffer, Length + tokens.Length);
        }

        public PartialOutput Append(IEnumerable<string> tokens)
        {
            var count = 0;
            foreach (var token in tokens) Buffer[Length + count++] = token;
            return new PartialOutput(Buffer, Length + count);
        }

        public IEnumerable<string> Output
        {
            get
            {
                for (var i = 0; i < Length; i++)
                    yield return Buffer[i];
            }
        }

        public string AsString => Output.Untokenize();

        public override string ToString() => AsString;
    }
}
