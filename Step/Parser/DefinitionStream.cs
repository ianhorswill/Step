#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefinitionStream.cs" company="Ian Horswill">
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
using Step.Interpreter;

namespace Step.Parser
{
    /// <summary>
    /// Reads a stream of method definitions from a TextReader.
    /// </summary>
    public class DefinitionStream
    {
        /// <summary>
        /// Reads definitions from the specified stream
        /// </summary>
        /// <param name="stream"></param>
        public DefinitionStream(TextReader stream) : this(new ExpressionStream(stream)) 
        { }

        /// <inheritdoc />
        public DefinitionStream(ExpressionStream expressions)
        {
            this.expressions = expressions.Expressions.GetEnumerator();
            MoveNext();
        }

        #region Stream interface
        /// <summary>
        /// Expressions being read from the stream
        /// </summary>
        private readonly IEnumerator<object> expressions;
        /// <summary>
        /// True if we've hit the end of the stream
        /// </summary>
        private bool end;
        /// <summary>
        /// Get the next expression from expressions, updating end.
        /// </summary>
        private void MoveNext()
        {
            end = !expressions.MoveNext();
        }

        /// <summary>
        /// Current expression
        /// </summary>
        private object Peek => expressions.Current;
        
        /// <summary>
        /// Return the current expression and move to the next
        /// </summary>
        /// <returns></returns>
        private object Get()
        {
            var result = Peek;
            MoveNext();
            return result;
        }

        /// <summary>
        /// Skip forward to the next token that isn't a newline
        /// </summary>
        private void SwallowNewlines()
        {
            while (!end && EndOfLineToken)
                Get();
        }
        #endregion

        #region End of definition tracking
        /// <summary>
        /// The current definition is a single line definition
        /// </summary>
        private bool multiLine;

        /// <summary>
        /// The actual token representation of an end of line
        /// </summary>
        private const string EndOfLine = "\n";

        /// <summary>
        /// True if we're at the end of the current definition
        /// </summary>
        private bool EndOfDefinition => end || ExplicitEndToken || (!multiLine && EndOfLineToken);

        /// <summary>
        /// True if we're at an end of line token
        /// </summary>
        private bool EndOfLineToken => Peek.Equals(EndOfLine);

        /// <summary>
        /// True if we're at an "[end]" expression
        /// </summary>
        private bool ExplicitEndToken => Peek is object[] array && array.Length == 1 && array[0].Equals("end");
        #endregion

        #region Source-language variables     
        /// <summary>
        /// True if the string is a valid local variable name
        /// </summary>
        private static bool IsLocalVariableName(object token) => token is string s && s.StartsWith("?");

        /// <summary>
        /// True if the string is a valid global variable name
        /// </summary>
        private static bool IsGlobalVariableName(object token) => token is string s && char.IsUpper(s[0]);

        /// <summary>
        /// Local variables of the definition currently being parsed.
        /// </summary>
        private readonly List<LocalVariableName> locals = new List<LocalVariableName>();

        /// <summary>
        /// Tokens being accumulated for the current Emit step of the current method.
        /// </summary>
        private readonly List<string> tokensToEmit = new List<string>();

        /// <summary>
        /// Return the local variable for the current method with the specified name,
        /// creating one and adding it to locals, if there isn't one.
        /// </summary>
        private LocalVariableName GetLocal(string name)
        {
            var result = locals.FirstOrDefault(l => l.Name == name);
            if (result == null)
            {
                result = new LocalVariableName(name, locals.Count);
                locals.Add(result);
            }

            return result;
        }
        #endregion

        /// <summary>
        /// Identify tokens that identify non-strings (variables, numbers) and replace them
        /// with their internal representations
        /// </summary>
        void Canonicalize(IList<object> objects)
        {
            for (var i = 0; i < objects.Count; i++)
                objects[i] = Canonicalize(objects[i]);
        }

        /// <summary>
        /// Return the internal representation for the term denoted by the specified token
        /// </summary>
        object Canonicalize(object o)
        {
            if (o is string s)
            {
                if (IsLocalVariableName(s))
                    return GetLocal(s);
                if (IsGlobalVariableName(s))
                    return GlobalVariableName.Named(s);
                if (int.TryParse(s, out var result))
                    return result;
            }
            else if (o is object[] list)
            {
                Canonicalize(list);
                return list;
            }

            return o;
        }

        /// <summary>
        /// Read, parse, and return the information for all method definitions in the stream
        /// </summary>
        public IEnumerable<(GlobalVariableName task, object[] pattern, LocalVariableName[] locals, Interpreter.Step chain)> Definitions
        {
            get
            {
                while (!end)
                    yield return ReadDefinition();
            }
        }

        /// <summary>
        /// Read and parse the next method definition
        /// </summary>
        private (GlobalVariableName task, object[] pattern, LocalVariableName[] locals, Interpreter.Step chain) ReadDefinition()
        {
            Interpreter.Step firstStep = null;
            Interpreter.Step previousStep = null;
            void AddStep(Interpreter.Step s)
            {
                if (firstStep == null)
                    firstStep = previousStep = s;
                else
                {
                    // ReSharper disable once PossibleNullReferenceException
                    previousStep.Next = s;
                    previousStep = s;
                }
            }

            locals.Clear();

            SwallowNewlines();

            // Process the head
            var taskName = Get() as string;
            if (taskName == null)
                throw new SyntaxError("Bracketed expression at start of definition");

            // Read the argument pattern
            var pattern = new List<object>();
            while (!Peek.Equals(":"))
            {
                var argPattern = Get();
                if (argPattern is object[] call)
                {
                    // it has an embedded predicate
                    pattern.Add(call[1]);
                    // Add the predicate to the body
                    AddStep(new Call(Canonicalize(call[0]), new []{ Canonicalize(call[1])}, null));
                }
                else
                    pattern.Add(argPattern);
            }
            Get(); // Swallow the colon
            
            // Change variable references in pattern to LocalVariableNames
            Canonicalize(pattern);

            multiLine = EndOfLineToken;
            if (multiLine)
                Get();  // Swallow the end of line
            
            // Read the body
            while (!EndOfDefinition)
            {
                tokensToEmit.Clear();
                while (!EndOfDefinition 
                       && Peek is string
                       && !IsLocalVariableName(Peek))
                    if (!EndOfLineToken)
                        tokensToEmit.Add((string)Get());
                    else
                    {
                        Get(); // Skip newline
                        if (EndOfLineToken)
                        {
                            // It's two consecutive newline tokens
                            tokensToEmit.Add((string)Get());
                        }
                    }

                if (tokensToEmit.Count > 0) 
                    AddStep(new EmitStep(tokensToEmit.ToArray(), null));

                if (!EndOfDefinition && IsLocalVariableName(Peek))
                {
                    AddStep(new Call(GetLocal((string)Peek), new object[0], null));
                    Get();
                }

                if (!EndOfDefinition && Peek is object[] expression)
                {
                    // It's a call
                    var targetName = expression[0] as string;
                    if (targetName == null)
                        throw new SyntaxError($"Invalid task name {expression[0]} in call.");
                    var target = IsLocalVariableName(targetName)
                        ? (object)GetLocal(targetName)
                        : GlobalVariableName.Named(targetName);
                    var args = expression.Skip(1).ToArray();
                    Canonicalize(args);
                    AddStep(new Call(target, args, null));
                    Get(); // Skip over the expression we just Peeked
                }
            }

            if (EndOfDefinition && !end)
                Get(); // Skip over the delimiter

            SwallowNewlines();

            return (GlobalVariableName.Named(taskName), pattern.ToArray(), locals.ToArray(), firstStep);
        }
    }
}
