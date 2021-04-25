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
using System.IO;
using System.Linq;

namespace Step.Parser
{
    /// <summary>
    /// Transforms a CSV file into a sequence of tokens (strings not containing whitespace) defining methods for a predicate.
    /// </summary>
    internal class CsvFileTokenStream : TokenStream
    {
        /// <summary>
        /// Make a new token stream reading from the specified text stream
        /// </summary>
        /// <param name="input">Stream to read from</param>
        /// <param name="filePath">Path of the stream if it comes from a file (for debug messages)</param>
        public CsvFileTokenStream(TextReader input, string filePath) : base(input, filePath)
        {
            predicateName = Path.GetFileNameWithoutExtension(filePath).Capitalize();
        }

        private readonly string predicateName;

        private char separator = ',';

        public override IEnumerable<string> Tokens
        {
            get
            {
                // ReSharper disable once UnusedVariable
                var header = GetRow();
                while (!End)
                {
                    yield return predicateName;
                    foreach (var cell in GetRow())
                        yield return cell.Replace(" ", "_");
                    yield return ".";
                    yield return "\n";
                    LineNumber++;
                }
            }
        }

        private string[] GetRow()
        {
            // ReSharper disable once PossibleNullReferenceException
            var line = input.ReadLine();
            if (LineNumber == 1 && line.Contains('\t'))
                separator = '\t';
            return line.Split(separator).Select(s => s.Trim(' ', '"')).ToArray();
        }
    }
}
