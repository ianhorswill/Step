﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Step.Parser
{
    internal abstract class TokenStream : IDisposable
    {
        public static TokenStream FromFile(string path)
        {
            var reader = File.OpenText(path);
            return Path.GetExtension(path) == ".csv"
                ? (TokenStream) new CsvFileTokenStream(reader, path)
                : new TextFileTokenStream(reader, path);
        }

        public static TokenStream FromReaderAndPath(TextReader reader, string? path)
        {
            if (path != null && Path.GetExtension(path) == ".csv")
                return new CsvFileTokenStream(reader, path);
            return new TextFileTokenStream(reader, path);
        }
        
        protected TokenStream(TextReader file, string? filePath)
        {
            FilePath = filePath;
            LineNumber = 1;
            Input = file;
        }

        protected TextReader Input;

        public void Dispose()
        {
            Input.Dispose();
        }
        
        /// <summary>
        /// Path to file being read from, if any
        /// </summary>
        public readonly string? FilePath;

        /// <summary>
        /// The Unicode left double quote
        /// </summary>
        public const char LeftDoubleQuote = '\u201C';

        /// <summary>
        /// The Unicode right double quote
        /// </summary>
        public const char RightDoubleQuote = '\u201D';

        public static readonly char[] QuoteChars = new[] { '"', LeftDoubleQuote, RightDoubleQuote };

        public static bool IsQuoteChar(char c) => Array.IndexOf(QuoteChars, c) >= 0;

        public static bool IsQuoteToken(object? t) => t is string { Length: 1 } s && IsQuoteChar(s[0]);

        /// <summary>
        /// Line number of file being read from
        /// </summary>
        public int LineNumber { get; protected set; }

        /// <summary>
        /// The stream of tokens read from the stream.
        /// </summary>
        public abstract IEnumerable<string> Tokens { get; }

        /// <summary>
        /// True if we're at the end of the stream
        /// </summary>
        protected bool End => Input.Peek() < 0;

        /// <summary>
        /// Return the current character, without advancing
        /// </summary>
        protected char Peek => (char)(Input.Peek());

        public bool LookingAt(char c) => Peek == c;
    }
}