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
        public DefinitionStream(TextReader stream) : this(new ExpressionStream(stream)) 
        { }

        public DefinitionStream(ExpressionStream expressions)
        {
            this.expressions = expressions.Expressions.GetEnumerator();
            MoveNext();
        }

        private readonly IEnumerator<object> expressions;
        private bool end;
        private void MoveNext()
        {
            end = !expressions.MoveNext();
        }

        private object Peek => expressions.Current;

        private static bool IsLocalVariableName(object token) => token is string s && s.StartsWith("?");
        private static bool IsGlobalVariableName(object token) => token is string s && char.IsUpper(s[0]);

        /// <summary>
        /// The current definition is a single line definition
        /// </summary>
        private bool multiLine;

        private const string EndOfLine = "\n";
        private bool EndOfDefinition => end || ExplicitEndToken || (!multiLine && EndOfLineToken);

        private bool EndOfLineToken => Peek.Equals(EndOfLine);
        private bool ExplicitEndToken => Peek is object[] array && array.Length == 1 && array[0].Equals("end");

        private object Get()
        {
            var result = Peek;
            MoveNext();
            return result;
        }

        private void SwallowNewlines()
        {
            while (!end && EndOfLineToken)
                Get();
        }

        private readonly List<LocalVariableName> locals = new List<LocalVariableName>();
        private readonly List<string> tokens = new List<string>();
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

        // ReSharper disable once IdentifierTypo
        void Variablize(IList<object> objects)
        {
            for (var i = 0; i < objects.Count; i++)
                objects[i] = Variablize(objects[i]);
        }

        // ReSharper disable once IdentifierTypo
        object Variablize(object o)
        {
            if (o is string s)
            {
                if (IsLocalVariableName(s))
                    return GetLocal(s);
                if (IsGlobalVariableName(s))
                    return GlobalVariable.Named(s);
                if (int.TryParse(s, out var result))
                    return result;
            }

            return o;
        }

        public IEnumerable<(GlobalVariable task, object[] pattern, LocalVariableName[] locals, Interpreter.Step chain)> Definitions
        {
            get
            {
                while (!end)
                    yield return ReadDefinition();
            }
        }

        private (GlobalVariable task, object[] pattern, LocalVariableName[] locals, Interpreter.Step chain) ReadDefinition()
        {
            locals.Clear();

            SwallowNewlines();

            // Process the head
            var taskName = Get() as string;
            if (taskName == null)
                throw new SyntaxError("Bracketed expression at start of definition");

            // Read the argument pattern
            var pattern = new List<object>();
            while (!Peek.Equals(":"))
                pattern.Add(Get());
            Get(); // Swallow the colon
            
            // Change variable references in pattern to LocalVariableNames
            Variablize(pattern);

            multiLine = EndOfLineToken;
            if (multiLine)
                Get();  // Swallow the end of line
            
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

            // Read the body
            while (!EndOfDefinition)
            {
                tokens.Clear();
                while (!EndOfDefinition && Peek is string)
                    if (!EndOfLineToken)
                        tokens.Add((string)Get());
                    else
                    {
                        Get(); // Skip newline
                        if (EndOfLineToken)
                        {
                            // It's two consecutive newline tokens
                            tokens.Add((string)Get());
                        }
                    }

                if (tokens.Count > 0) 
                    AddStep(new EmitStep(tokens.ToArray(), null));
                if (!EndOfDefinition && Peek is object[] expression)
                {
                    // It's a call
                    var targetName = expression[0] as string;
                    if (targetName == null)
                        throw new SyntaxError($"Invalid task name {expression[0]} in call.");
                    var target = IsLocalVariableName(targetName)
                        ? (object)GetLocal(targetName)
                        : GlobalVariable.Named(targetName);
                    var args = expression.Skip(1).ToArray();
                    Variablize(args);
                    AddStep(new Call(target, args, null));
                    Get(); // Skip over the expression we just Peeked
                }
            }

            if (EndOfDefinition && !end)
                Get(); // Skip over the delimiter

            SwallowNewlines();

            return (GlobalVariable.Named(taskName), pattern.ToArray(), locals.ToArray(), firstStep);
        }
    }
}
