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
using System.Linq;
using System.Text;

namespace Step.Output
{
    /// <summary>
    /// Random utilities for working with text
    /// </summary>
    public static class TextUtilities
    {
        /// <summary>
        /// Internal token used to signify the start of a new line
        /// </summary>
        public const string NewLineToken = "--newline--";
        /// <summary>
        /// Internal token used to signify the start of a new paragraph
        /// </summary>
        public const string NewParagraphToken = "--paragraph--";
        /// <summary>
        /// Internal token used to signify that any future tokens should start on a fresh line.
        /// </summary>
        public const string FreshLineToken = "--fresh line--";
        /// <summary>
        /// A token that forces a space between two tokens that wouldn't otherwise have have spaces between them.
        /// </summary>
        public const string ForceSpaceToken = " ";

        private static readonly string[] NoSpaceAfterTokens = {"-", "\n", "\"", "\u201c" /* left double quote */ };

        private static bool LineChange(string token) => ReferenceEquals(token, NewLineToken) || ReferenceEquals(token, NewParagraphToken);
        private static bool NoSpaceAfter(string token) => NoSpaceAfterTokens.Contains(token) || LineChange(token) || ReferenceEquals(token, ForceSpaceToken);
        
        private static bool LineEnding(string token) => token == "." || LineChange(token);

        private static readonly string[] Abbreviations = {"Mr", "Ms", "Mrs", "Dr"};
        /// <summary>
        /// Convert a sequence of tokens into a single text string, adding spaces where appropriate.
        /// </summary>
        public static string Untokenize(this IEnumerable<string> tokens, FormattingOptions format = null)
        {
            if (tokens == null)
                return "";
            
            if (format == null)
                format = FormattingOptions.Default;
            
            var b = new StringBuilder();
            var firstOne = true;
            var lastToken = "";
            var forceFreshLine = false;
            foreach (var t in TokenFilter.ApplyFilters(format.TokenFilters, tokens))
            {
                if (TokenFilter.IsControlToken(t))
                    continue;

                if (firstOne 
                    && format.SuppressLeadingVerticalSpace 
                    && (t == NewLineToken || t == FreshLineToken || t == NewParagraphToken))
                    continue;
                
                if (t == null)
                    continue;

                var token = t;
                if (ReferenceEquals(token, FreshLineToken))
                {
                    forceFreshLine = true;
                    continue;
                }

                if (forceFreshLine && !LineChange(lastToken) && !LineChange(token) && !firstOne)
                {
                    b.Append(format.LineSeparator);
                    lastToken = NewLineToken;
                }
                forceFreshLine = false;
                
                if (t != "" && (!PunctuationToken(t) || t == "\u201c") && !t.StartsWith("<") && t != "\n")
                {
                    if (format.Capitalize && (firstOne || LineEnding(lastToken)) && char.IsLower(t[0]))
                        token = token.Capitalize();
                    if (firstOne)
                        firstOne = false;
                    else if (!NoSpaceAfter(lastToken) && !lastToken.StartsWith("<")  && !lastToken.EndsWith("'") && !LineChange(token)
                                && !ReferenceEquals(token, ForceSpaceToken))
                        b.Append(' ');
                    if (format.FrenchSpacing && lastToken == "."  && !LineChange(lastToken))
                        // Double the space after period.
                        b.Append(' ');
                }

                if (lastToken == "," && token == "\"")
                    b.Append(' ');

                if (ReferenceEquals(token, NewParagraphToken))
                {
                    if (!ReferenceEquals(lastToken, NewParagraphToken))
                        b.Append(format.ParagraphMarker);
                }
                else if (ReferenceEquals(token, NewLineToken))
                    b.Append(format.LineSeparator);
                else
                    b.Append(token);
                
                if (!t.StartsWith("<") && !(lastToken == "." && token == "\"") && !(token == "." && Abbreviations.Contains(lastToken)))
                    lastToken = token;
            }

            return b.ToString();
        }

        /// <summary>
        /// Force first character of token to be capitalized.
        /// </summary>
        public static string Capitalize(this string token)
        {
            var a = token.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        /// <summary>
        /// True when string consists of just a single punctuation mark.
        /// </summary>
        private static bool PunctuationToken(string s) => s.Length == 1 && char.IsPunctuation(s[0]);
        
        /// <summary>
        /// True if the first character of the string is a vowel.
        /// </summary>
        public static bool StartsWithVowel(string x)
        {
            // ReSharper disable once StringLiteralTypo
            return x.Length > 0 && "aeiou".Contains(x[0]);
        }
    }
}
