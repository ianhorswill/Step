using System;
using System.Collections;
using System.IO;
using System.Text;
using Step;
using Step.Interpreter;
using Step.Utilities;

namespace StepRepl.Importers
{
    public class SExpressionReader
    {
        public static void AddBuiltins(Module m)
        {
            Documentation.SectionIntroduction("S-Expressions", "Tasks for manipulating s-expression files.");
            m["ReadLisp"] = new SimpleFunction<string, object>(
                    "ReadLisp",
                    fileName => ReadFile(CanonicalizePath(fileName, ".lisp")))
                .Arguments("filename", "?object")
                .Documentation("S-Expressions", "Reads filename.lisp and places the decoded data in ?object.");

            Documentation.SectionIntroduction("S-Expressions", "Tasks for manipulating s-expression files.");
            m["ReadPddl"] = new SimpleFunction<string, object>(
                    "ReadPddl",
                    fileName => ReadFile(CanonicalizePath(fileName, ".pddl")))
                .Arguments("filename", "?object")
                .Documentation("S-Expressions", "Reads filename.pddl and places the decoded data in ?object.");
        }

        public static string CanonicalizePath(string path, string defaultExtension)
        {
            var qualified = Equals(path, Path.GetFileName(path)) ? Path.Combine(StepCode.ProjectDirectory, path) : path;
            return Path.GetExtension(qualified) == null ? qualified + defaultExtension : qualified;
        }

        /// <summary>
        /// Read an s-expression from a file
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>Object (list, symbol, etc.) in the file</returns>
        public static object ReadFile(string path)
        {
            using (var f = File.OpenText(path))
            {
                return new SExpressionReader(f).Read();
            }
        }

        private readonly TextReader input;

        public SExpressionReader(TextReader input)
        {
            this.input = input;
        }

        /// <summary>
        /// Read the next object in the file, be it a list, number or whatever.
        /// </summary>
        /// <returns></returns>
        public object Read()
        {
            SkipWhiteSpace();
            CheckEnd();

            switch (Peek)
            {
                case '(':
                    return ReadList();

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '+':
                case '-':
                case '.':
                    return ReadNumber();

                case '"':
                    return ReadQuotedString();

                default:
                    return ReadSymbol();
            }
        }

        private object ReadList()
        {
            var items = new ArrayList();
            Get();    // swallow (
            SkipWhiteSpace();
            CheckEnd();
            while (Peek != ')')
            {
                items.Add(Read());
                SkipWhiteSpace();
                CheckEnd();
            }

            Get();
            return items.ToArray();
        }

        private object ReadQuotedString()
        {
            var str = new StringBuilder();
            Get();    // swallow "
            CheckEnd();
            while (Peek != '"')
            {
                if (Peek == '\\')
                {
                    Get(); // skip \
                    CheckEnd();
                }
                str.Append(Read());
                CheckEnd();
            }

            Get();  // Swallow "
            return str.ToString();
        }

        private object ReadSymbol()
        {
            return GetToken(c => char.IsLetterOrDigit(c) || c == '-' || c == ':');
        }

        private object ReadNumber()
        {
            var token = GetToken(c => char.IsDigit(c) || c == '-' || c == '+' || c == '.');
            if (token.Contains("."))
                return float.Parse(token);
            return int.Parse(token);
        }

        // Skip over any whitespace
        // Postcondition: we at the next non-whitespace character
        private void SkipWhiteSpace()
        {
            while (!End && char.IsWhiteSpace(Peek))
                Get();
        }

        // Return the next character without consuming it
        protected char Peek
        {
            get
            {
                PossiblySkipComment();
                return (char) input.Peek();
            }
        }

        // Consume the next character and return it
        protected char Get()
        {
            PossiblySkipComment();
            return (char) input.Read();
        }

        // True if we've consumed all the characters in the file
        protected bool End => input.Peek() < 0;

        // If we're at a comment, skip over it
        private void PossiblySkipComment()
        {
            if (Peek != ';')
                // This isn't a comment
                return;

            // Skip the comment
            do
            {
                Get();
            } while (Peek != '\n');

            Get();

            if (Peek != '\r')
                Get();
        }

        /// <summary>
        /// Throw an exception if we're at EOF
        /// </summary>
        protected void CheckEnd()
        {
            if (End)
                throw new Exception("Unexpected end of file while reading s-expression");
        }

        private readonly StringBuilder tokenBuffer = new StringBuilder();

        /// <summary>
        /// The a contiguous sequence of characters satisfying the specified predicate and return it
        /// </summary>
        /// <param name="p">Criterion for accepting characters</param>
        /// <returns>String of consumed characters</returns>
        protected string GetToken(Predicate<char> p)
        {
            while (!End && p(Peek))
                tokenBuffer.Append(Get());
            var token = tokenBuffer.ToString();
            tokenBuffer.Length = 0;
            return token;
        }
    }
}
