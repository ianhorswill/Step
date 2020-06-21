#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TokenStream.cs" company="Ian Horswill">
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
using System.IO;
using System.Text;

namespace Step.Parser
{
    /// <summary>
    /// Transforms a stream of characters into a sequence of tokens (strings not containing whitespace)
    /// </summary>
    public class TokenStream
    {
        private readonly TextReader input;
        private readonly StringBuilder token = new StringBuilder();

        public TokenStream(TextReader input)
        {
            this.input = input;
        }

        private bool HaveToken => token.Length > 0;
        void AddCharToToken() => token.Append(Get());

        string ConsumeToken()
        {
            var newToken = token.ToString();
            token.Length = 0;
            return newToken;
        }

        char Get() => (char) (input.Read());

        void Skip() => Get();

        void SkipWhitespace()
        {
            while (IsWhiteSpace) Skip();
        }

        char Peek => (char) (input.Peek());
        bool End => input.Peek() < 0;
        bool IsWhiteSpace => char.IsWhiteSpace(Peek)  && Peek != '\n';
        private bool IsPunctuation => char.IsPunctuation(Peek) && Peek != '?';
        private bool IsEndOfWord
        {
            get        
            {
                var c = Peek;
                return char.IsWhiteSpace(c) || char.IsPunctuation(c);
            }
        }



        public IEnumerable<string> Tokens
        {
            get
            {
                while (!End)
                {
                    SkipWhitespace();
                    // Start of token
                    Debug.Assert(token.Length == 0);
                    // Handle any single-character tokens
                    while (IsPunctuation || Peek == '\n')
                        yield return Get().ToString();
                    // Allow ?'s at the start of word tokens
                    if (Peek == '?')
                        AddCharToToken();
                    // Now we should be at something like a word or number
                    while (!End && !IsEndOfWord)
                        AddCharToToken();
                    if (HaveToken)
                        yield return ConsumeToken();
                }
            }
        }
    }
}
