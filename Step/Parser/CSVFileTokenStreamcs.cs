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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Step.Output;

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

        /// <summary>
        /// If true, treat capitalized tokens in cells as strings rather than global variables
        /// </summary>
        public bool EscapeStrings = true;

        private static readonly string[] TrueValues = {"yes", "y", "true", "t", "X"};

        private readonly string predicateName;

        public override IEnumerable<string> Tokens
        {
            get
            {
                bool UnaryPredicateColumn(string name) => name.EndsWith("?");
                bool BinaryPredicateColumn(string name) => name.StartsWith("@");

                string FormatCellToken(string str) => EscapeToken(str.Trim().Replace(" ", "_"));

                IEnumerable<string> FormatCell(string cell)
                {
                    if (cell.Contains(':'))
                    {
                        yield return "[";
                        foreach (var element in cell.Split(':'))
                        foreach (var token in FormatCell(element))
                            yield return token;
                        yield return "]";
                    }
                    else 
                        yield return FormatCellToken(cell);

                }

                //
                // Process header
                //
                var header = GetRow();
                if (header.Length == 0)
                    throw new SyntaxError("Zero-length header row in CSV file", FilePath, LineNumber);
                var normalArgs = header.Length;
                for (int i = 0; i < header.Length; i++)
                {
                    var column = header[i];
                    if (UnaryPredicateColumn(column) || BinaryPredicateColumn(column))
                    {
                        normalArgs = i;
                        break;
                    }
                }

                while (!End)
                {
                    //
                    // Read a row
                    //
                    //var row = GetRow().Select(cell => cell.Replace(" ", "_")).ToArray();
                    var row = GetRow();
                    if (row.Length != header.Length)
                        throw new SyntaxError($"Row has {row.Length} columns, but header has {header.Length}", FilePath,
                            LineNumber);
                    var rowItem = FormatCell(row[0]).ToArray();
                    var col = 0;

                    if (header[0] == "[#]")
                    {
                        var weight = row[col++].Trim();
                        if (weight != "")
                        {
                            yield return "[";
                            yield return weight;
                            yield return "]";
                        }
                    }

                    // Generate the method for the main predicate
                    yield return predicateName;
                    for (; col < normalArgs; col++)
                    {
                        var heading = header[col];
                        var cell = row[col];
                        if (heading.EndsWith("[]") || heading.EndsWith("[/]"))
                        {
                            var eltSeparator = heading.EndsWith("[/]") ? '/' : ' ';
                            yield return "[";
                            foreach (var e in cell.Trim().Split(eltSeparator))
                                if (!string.IsNullOrEmpty(e))
                                    foreach (var token in FormatCell(e))
                                        yield return token;
                            yield return "]";
                        }
                        else 
                            foreach (var token in FormatCell(cell))
                                yield return token;
                    }

                    yield return ".";
                    yield return "\n";

                    // Generate extra unary or binary predicates from remaining columns, if any
                    for (; col < row.Length; col++)
                    {
                        var columnHeading = header[col];
                        var rawCell = row[col].Trim();
                        if (UnaryPredicateColumn(columnHeading))
                        {
                            if (TrueValues.Any(v => v.Equals(rawCell, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                yield return columnHeading.Replace("?", "");
                                foreach (var token in rowItem)
                                    yield return token;
                                yield return ".";
                                yield return "\n";
                            }
                        }
                        else if (BinaryPredicateColumn(columnHeading))
                        {
                            yield return columnHeading.Replace("@", "");
                            foreach (var token in rowItem)
                                yield return token;
                            foreach (var token in FormatCell(row[col]))
                                yield return token;
                            yield return ".";
                            yield return "\n";
                        }
                        else
                            throw new SyntaxError($"Unknown column format: {columnHeading}", FilePath, 1);
                    }

                    LineNumber++;
                }
            }
        }

        private readonly StringBuilder stringBuffer = new StringBuilder();
        private readonly List<string> rowBuffer = new List<string>();

        bool EndOfLineChar
        {
            get
            {
                var next = Input.Peek();
                return next == '\n' || next == '\r';
            }
        }

        /// <summary>
        /// Read a row from the spreadsheet.
        /// </summary>
        /// <returns>Array of column strings</returns>
        private string[] GetRow()
        {
            void FinishColumn()
            {
                rowBuffer.Add(stringBuffer.ToString());
                stringBuffer.Clear();
            }

            stringBuffer.Clear();
            rowBuffer.Clear();

            while (!EndOfLineChar && !End)
            {
                char ch;
                switch (ch = (char) Input.Read())
                {
                    case '\n':
                        case '\r':
                        break;

                    case ',': 
                    case '\t':
                        FinishColumn();
                        break;

                    case '"':
                        ReadQuoted();
                        break;

                    default:
                        stringBuffer.Append(ch);
                        break;
                }
            }

            // Now at EOL.
            while (EndOfLineChar) Input.Read();

            rowBuffer.Add(stringBuffer.ToString());
            stringBuffer.Clear();

            return rowBuffer.ToArray();

            // ReSharper disable once PossibleNullReferenceException
            //var line = Input.ReadLine();
            //Debug.Assert(line != null, nameof(line) + " != null");
            //if (LineNumber == 1 && line.Contains('\t'))
            //    separator = '\t';
            //return line.Split(separator).Select(s => s.Trim(' ', '"')).ToArray();
        }

        /// <summary>
        /// Read a quoted column
        /// This just reads into the string buffer.  We depend on the caller to move the string buffer into the row buffer.
        /// </summary>
        private void ReadQuoted()
        {
            while (!EndOfLineChar && !End)
            {
                char ch;
                if ((ch = (char)Input.Read()) == '"')
                {
                    if (Input.Peek() == '"')
                    {
                        // It's an escaped quote mark
                        stringBuffer.Append('"');
                        Input.Read(); // Skip second quote mark
                    }
                    else 
                        // Quote marks the end of the column
                        break;
                }
                else
                    stringBuffer.Append(ch);
            }
        }

        private string EscapeToken(string s) 
            => (EscapeStrings && !string.IsNullOrEmpty(s) && char.IsUpper(s[0]))
                ? TextFileTokenStream.EscapeStringToken(s)
                : s;

    }
}
