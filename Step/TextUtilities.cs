#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextUtilities.cs" company="Ian Horswill">
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
using System.Text;

namespace Step
{
    /// <summary>
    /// Random utilities for working with text
    /// </summary>
    public static class TextUtilities
    {
        /// <summary>
        /// Convert a sequence of tokens into a single text string, adding spaces where appropriate.
        /// </summary>
        public static string Untokenize(this IEnumerable<string> tokens, bool capitalize = true, bool frenchSpacing = true)
        {
            if (tokens == null)
                return "";
            var b = new StringBuilder();
            var firstOne = true;
            var lastToken = "";
            foreach (var t in tokens)
            {
                var token = t;
                if (!PunctuationToken(t) && !t.StartsWith("<") && t != "\n")
                {
                    if (capitalize && (firstOne || lastToken == "." && char.IsLower(t[0])))
                        token = Capitalize(token);
                    if (firstOne)
                        firstOne = false;
                    else if (lastToken != "-" && lastToken != "\n" && !lastToken.StartsWith("<"))
                        b.Append(' ');
                    if (frenchSpacing && lastToken == ".")
                        // Double the space after period.
                        b.Append(' ');
                }

                b.Append(token);
                if (!t.StartsWith("<"))
                    lastToken = token;
            }

            return b.ToString();
        }

        /// <summary>
        /// Force first character of token to be capitalized.
        /// </summary>
        private static string Capitalize(string token)
        {
            var a = token.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        /// <summary>
        /// True when string consists of just a single punctuation mark.
        /// </summary>
        private static bool PunctuationToken(string s) => s.Length == 1 && char.IsPunctuation(s[0]);
    }
}
