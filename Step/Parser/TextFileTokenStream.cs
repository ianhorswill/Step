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
using System.Linq;
using System.Text;
using Step.Output;

namespace Step.Parser
{
    /// <summary>
    /// Transforms a stream of characters into a sequence of tokens (strings not containing whitespace)
    /// </summary>
    internal class TextFileTokenStream : TokenStream
    {
        public static string[] Tokenize(string code)
        {
            var stream = new TextFileTokenStream(new StringReader(code), null);
            return stream.Tokens.ToArray();
        }
        
        /// <summary>
        /// Make a new token stream reading from the specified text stream
        /// </summary>
        /// <param name="input">Stream to read from</param>
        /// <param name="filePath">Path of the stream if it comes from a file (for debug messages)</param>
        public TextFileTokenStream(TextReader input, string filePath) : base(input, filePath)
        { }

        #region Token buffer managment
        /// <summary>
        /// Buffer for accumulating characters into tokens
        /// </summary>
        private readonly StringBuilder token = new StringBuilder();

        /// <summary>
        /// True it token buffer non-empty
        /// </summary>
        private bool HaveToken => token.Length > 0;

        /// <summary>
        /// Add current stream character to token
        /// </summary>
        void AddCharToToken(bool ignoreComments = true) => token.Append(Get(ignoreComments));

        /// <summary>
        /// Return the accumulated characters as a token and clear the token buffer.
        /// </summary>
        string ConsumeToken(bool quoted = false)
        {
            var newToken = token.ToString();
            token.Length = 0;
            return quoted?EscapeStringToken(newToken):newToken;
        }
        #endregion

        /// <summary>
        /// Return the current character and advance to the next
        /// </summary>
        /// <returns></returns>
        private char Get(bool ignoreComments = true)
        {
            var c = (char) Input.Read();
            switch (c)
            {
                case '\n':
                    LineNumber++;
                    break;

                case '#':
                    if (ignoreComments)
                    {
                        // Swallow line
                        while (!End && Peek != '\n') Input.Read();
                        Input.Read();
                        return '\n';
                    }
                    else 
                        return '#';

                case '"':
                    var nextCode = Input.Peek();
                    var next = (char) nextCode;
                    if (nextCode < 0 || char.IsWhiteSpace(next) || char.IsPunctuation(next))
                        return RightDoubleQuote;
                    else
                        return LeftDoubleQuote;
            }

            return c;
        }

        /// <summary>
        /// Synonym for Get().  Used to indicate the character is being deliberately thrown away.
        /// </summary>
        private void Skip() => Get();

        /// <summary>
        /// Skip over all whitespace chars, except newlines (they're considered tokens)
        /// </summary>
        void SkipWhitespace()
        {
            while (IsWhiteSpace)
                Skip();
        }

        #region Character classification
        /// <summary>
        /// Current character is non-newline whitespace
        /// </summary>
        bool IsWhiteSpace => (char.IsWhiteSpace(Peek) && Peek != '\n');

        /// <summary>
        /// Current character is some punctuation symbol other than '?'
        /// '?' is treated specially because it's allowed to start a variable-name token.
        /// </summary>
        private bool IsPunctuationNotSpecial => MyIsPunctuation(Peek) && !SpecialPunctuation.Contains(Peek);

        private static readonly char[] SpecialPunctuation = new[] {'?', '^', '<', '>','+', '-', '|'};

        private static bool MyIsPunctuation(char c) => c != '_' && c != '\\' && (char.IsPunctuation(c) || char.IsSymbol(c));

        /// <summary>
        /// True if the current character can't be a continuation of a word token.
        /// </summary>
        private bool IsEndOfWord
        {
            get        
            {
                var c = Peek;
                return char.IsWhiteSpace(c) || MyIsPunctuation(c);
            }
        }
        #endregion

        /// <summary>
        /// The stream of tokens read from the stream.
        /// </summary>
        public override IEnumerable<string> Tokens
        {
            get
            {
                while (!End)
                {
                    SkipWhitespace();
                    // Start of token
                    Debug.Assert(token.Length == 0);

                    // SINGLE CHARACTER TOKENS
                    while (IsPunctuationNotSpecial || Peek == '\n')
                    {
                        var ch = Get();
                        if (ch == '\n' && (Peek == '\r' || Peek == '\n'))
                            yield return TextUtilities.NewParagraphToken;
                        else
                            yield return ch.ToString();
                    }

                    if (Peek == '>')
                    {
                        AddCharToToken();
                        if (Peek == '=')
                            AddCharToToken();
                    }
                    // HTML TAGS
                    else if (Peek == '<')
                    {
                        AddCharToToken();
                        if (Peek == '-' || Peek == '=')
                            //  It's a <- or <= operator
                            AddCharToToken();
                        else if (Peek == '/' || char.IsLetter(Peek))
                        {
                            // It's an HTML markup token
                            while (!End && Peek != '>')
                                AddCharToToken();
                            if (Peek == '>')
                                AddCharToToken();
                        }
                    }
                    // NUMBERS
                    else if (char.IsDigit(Peek) || Peek == '+' || Peek == '-')
                    {
                        AddCharToToken();
                        while (char.IsDigit(Peek)) AddCharToToken();

                        if (Peek == '.')
                        {
                            Get();
                            if (char.IsDigit(Peek))
                            {
                                token.Append('.');
                                while (char.IsDigit(Peek)) AddCharToToken();
                            }
                            else
                            {
                                // This was an integer followed by a period.  "1." and ".1" are not valid
                                // floats in this language.
                                yield return ConsumeToken();
                                yield return ".";
                            }
                        }
                    }
                    else if (Peek == '|')
                    {
                        Get();   // Skip | symbol
                        if (HaveToken)
                            ConsumeToken();
                        // It's a quoted symbol/string, as opposed to text
                        while (!End && Peek != '|')
                        {
                            if (Peek == '\\')
                            {
                                Get();
                                if (End)
                                    throw new SyntaxError("File ends with a backslash escape", FilePath, LineNumber);
                            }
                            AddCharToToken(false);
                        }

                        if (End)
                            throw new SyntaxError("File ends with a backslash escape", FilePath, LineNumber);
                        Get(); // Skip |

                        yield return ConsumeToken(true);
                    }
                    // NORMAL TOKENS (word-like tokens and variables
                    else
                    {
                        // Allow ?'s and ^'s at the start of word tokens
                        if (Peek == '?' || Peek == '^')
                            AddCharToToken();
                        // Now we should be at something like a word or number
                        while (!End && !IsEndOfWord)
                        {
                            if (Peek == '\\')
                            {
                                Get();
                                if (End)
                                    throw new SyntaxError("File ends with backslash escape", FilePath, LineNumber);
                                AddCharToToken(false);
                            }
                            else 
                                AddCharToToken();
                        }
                    }

                    // Yield the token
                    if (HaveToken)
                        yield return ConsumeToken();
                }
            }
        }

        public static bool IsEscapedStringToken(string s) => s.StartsWith(" ");
        public static string EscapeStringToken(string s) => " " + s;
        public static string UnescapeStringToken(string s) => s.Substring(1);
    }
}
