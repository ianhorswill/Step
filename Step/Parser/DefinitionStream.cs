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
        public DefinitionStream(TextReader stream, Module module, string filePath)
            : this(new ExpressionStream(stream, filePath), module) 
        { }

        /// <inheritdoc />
        public DefinitionStream(ExpressionStream expressions, Module module)
        {
            Module = module;
            expressionStream = expressions;
            this.expressions = expressions.Expressions.GetEnumerator();
            MoveNext();
        }

        /// <summary>
        /// Module into which this is reading definitions
        /// </summary>
        public readonly Module Module;

        #region Stream interface

        private readonly ExpressionStream expressionStream;

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

        private readonly List<ushort> referenceCounts = new List<ushort>();

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
            if (name == "?")
                return GetFreshLocal(name);

            var result = locals.FirstOrDefault(l => l.Name == name);
            return result ?? GetFreshLocal(name);
        }

        private LocalVariableName GetFreshLocal(string name)
        {
            var local = new LocalVariableName(name, locals.Count);
            locals.Add(local);
            referenceCounts.Add(0);
            return local;
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
                {
                    var v = GetLocal(s);
                    referenceCounts[v.Index] += 1;
                    return v;
                }
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
        public IEnumerable<(GlobalVariableName task, object[] pattern, LocalVariableName[] locals, Interpreter.Step chain,
                string path, int lineNumber)>
            Definitions
        {
            get
            {
                while (!end)
                    yield return ReadDefinition();
            }
        }

        private Interpreter.Step firstStep;
        private Interpreter.Step previousStep;

        private int lineNumber;
        
        private void InitParserState()
        {
            locals.Clear();
            referenceCounts.Clear();
            firstStep = previousStep = null;
        }

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

        /// <summary>
        /// Read and parse the next method definition
        /// </summary>
        private (GlobalVariableName task, object[] pattern, LocalVariableName[] locals, Interpreter.Step chain,
            string path, int lineNumber) ReadDefinition()
        {
            InitParserState();
            SwallowNewlines();
            lineNumber = expressionStream.LineNumber;

            // READ HEAD
            var (taskName, pattern) = ReadHead();
            
            // READ BODY
            while (!EndOfDefinition)
            {
                TryProcessTextBlock();
                TryProcessMentionExpression();
                TryProcessMethodCall();
            }
            
            CheckForWarnings();

            // Eat end token
            if (EndOfDefinition && !end)
                Get(); // Skip over the delimiter
            SwallowNewlines();

            return (GlobalVariableName.Named(taskName), pattern.ToArray(), locals.ToArray(), firstStep, expressionStream.FilePath, lineNumber);
        }
        
        /// <summary>
        /// Read the task name and argument pattern
        /// </summary>
        private (string taskName, List<object> pattern) ReadHead()
        {
            // Get the task name
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
                    // It has an embedded predicate
                    pattern.Add(call[1]);
                    AddStep(new Call(Canonicalize(call[0]), new[] {Canonicalize(call[1])}, null));
                }
                else
                    pattern.Add(argPattern);
            }

            // Change variable references in pattern to LocalVariableNames
            Canonicalize(pattern);

            // SKIP COLON
            Get();

            multiLine = EndOfLineToken;
            if (multiLine)
                Get();  // Swallow the end of line

            return (taskName, pattern);
        }
        
        /// <summary>
        /// If we're looking at a method call, compile it.
        /// </summary>
        private void TryProcessMethodCall()
        {
            if (EndOfDefinition || !(Peek is object[] expression)) 
                return;

            // It's a call
            var targetName = expression[0] as string;
            switch (targetName)
            {
                case null:
                    throw new SyntaxError($"Invalid task name {expression[0]} in call.");

                case "set":
                    if (expression.Length != 3)
                        throw new ArgumentCountException("set", 2, expression.Skip(1).ToArray());
                    var name = expression[1] as string;
                    if (name == null || !IsGlobalVariableName(name))
                        throw new SyntaxError(
                            $"A Set command can only update a GlobalVariable; it can't update {expression[1]}");
                    AddStep(new AssignmentStep(GlobalVariableName.Named(name), Canonicalize(expression[2]), null));
                    break;
                    
                default:
                    // This is a call
                    var target = IsLocalVariableName(targetName)
                        ? (object) GetLocal(targetName)
                        : GlobalVariableName.Named(targetName);
                    var args = expression.Skip(1).Where(token => !token.Equals("\n")).ToArray();
                    Canonicalize(args);
                    AddStep(new Call(target, args, null));
                    break;
            }

            Get(); // Skip over the expression we just Peeked
        }

        /// <summary>
        /// If we're looking at a mention expression (?x, ?x/Foo, ?x/Foo/Bar, etc.), compile it.
        /// </summary>
        private void TryProcessMentionExpression()
        {
            if (EndOfDefinition || !IsLocalVariableName(Peek))
                return;

            var local = GetLocal((string) Get());

            if (!Peek.Equals("/"))
            {
                // This is a simple variable mention
                AddStep(new Call(local, new object[0], null));
                return;
            }

            ReadComplexMentionExpression(local);
        }

        /// <summary>
        /// Read an expression of the form ?local/STUFF
        /// Called after ?local has already been read.
        /// </summary>
        /// <param name="local">The variable before the /</param>
        private void ReadComplexMentionExpression(LocalVariableName local)
        {
// This is a complex "/" expression
            while (Peek.Equals("/"))
            {
                Get(); // Swallow slash
                var t = Get();
                if (!(t is string targetName))
                    throw new SyntaxError($"Invalid method name after the /: {local}/{t}");
                var target = IsLocalVariableName(targetName)
                    ? (object) GetLocal(targetName)
                    : GlobalVariableName.Named(targetName);
                if (Peek.Equals("/"))
                {
                    var tempVar = GetFreshLocal("temp");
                    // There's another / coming so this is a function call
                    AddStep(new Call(target, new object[] {local, tempVar}, null));
                    local = tempVar;
                }
                else
                {
                    ReadMentionExpressionTail(local, target);
                }
            }
        }

        /// <summary>
        /// Called after the last "/" of a complex mention expression.
        /// </summary>
        /// <param name="local">Result of the expression from before the last "/"</param>
        /// <param name="targetVar">Task to call on local</param>
        /// <param name="t"></param>
        private void ReadMentionExpressionTail(LocalVariableName local, object targetVar)
        {
            AddStep(new Call(targetVar, new object[] {local}, null));
            while (Peek.Equals("+"))
            {
                Get(); // Swallow slash
                var targetToken = Get();
                if (!(targetToken is string targetName))
                    throw new SyntaxError($"Invalid method name after the /: {local}/{targetToken}");
                var target = IsLocalVariableName(targetName)
                    ? (object) GetLocal(targetName)
                    : GlobalVariableName.Named(targetName);
                AddStep(new Call(target, new object[] {local}, null));
            }
        }

        /// <summary>
        /// If this is a sequence of fixed text tokens, compile them into an EmitStep.
        /// </summary>
        private void TryProcessTextBlock()
        {
            tokensToEmit.Clear();
            while (!EndOfDefinition && Peek is string && !IsLocalVariableName(Peek))
                if (EndOfLineToken)
                {
                    // Skip unless double newline
                    Get();
                    if (EndOfLineToken)
                        tokensToEmit.Add((string) Get());
                }
                else
                    tokensToEmit.Add((string) Get());

            if (tokensToEmit.Count > 0)
                AddStep(new EmitStep(tokensToEmit.ToArray(), null));
        }

        /// <summary>
        /// Issue warnings for any singleton variables
        /// </summary>
        private void CheckForWarnings()
        {
            for (var i = 0; i < locals.Count; i++)
                if (referenceCounts[i] == 1 && !locals[i].Name.StartsWith("?"))
                    Module.AddWarning(
                        $"{Path.GetFileName(expressionStream.FilePath)}:{lineNumber} Singleton variable {locals[i].Name}");
        }
    }
}
