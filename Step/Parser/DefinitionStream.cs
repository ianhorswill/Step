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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Step.Interpreter;

namespace Step.Parser
{
    /// <summary>
    /// Reads a stream of method definitions from a TextReader.
    /// </summary>
    internal class DefinitionStream
    {
        /// <summary>
        /// Reads definitions from the specified stream
        /// </summary>
        public DefinitionStream(TextReader stream, Module module, string filePath)
            : this(new ExpressionStream(stream, filePath), module) 
        { }

        ///
        /// Make a new definition stream
        /// 
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

        public string SourceFile
        {
            get
            {
                var path = expressionStream.FilePath;
                return path == null ? "Unknown" : Path.GetFileName(path);
            }
        }

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
        private bool ExplicitEndToken => KeywordMarker("end");

        private bool KeywordMarker(string keyword) =>
            Peek is object[] array && array.Length == 1 && array[0].Equals(keyword);

        private bool OneOfKeywordMarkers(params string[] keywords) =>
            Peek is object[] array && array.Length == 1 && array[0] is string k && Array.IndexOf(keywords, k) >= 0;

        private readonly string[] keywordMarkers = new[] {"end", "or", "else", "then" };
        private bool AtKeywordMarker => OneOfKeywordMarkers(keywordMarkers);

        private readonly string[] caseBranchKeywords = new[] {"end", "or", "else", "then" };
        private bool EndCaseBranch() => OneOfKeywordMarkers(caseBranchKeywords);

        private bool ElseToken => KeywordMarker("else");
        #endregion

        #region Source-language variables     
        /// <summary>
        /// True if the string is a valid local variable name
        /// </summary>
        private static bool IsLocalVariableName(object token) => token is string s && s.StartsWith("?");

        /// <summary>
        /// The token is a valid name of a non-anonymous local variable
        /// </summary>
        private static bool IsNonAnonymousLocalVariableName(object token) => token is string s && s.StartsWith("?") && s.Any(char.IsLetter);

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
        object[] CanonicalizeArglist(IEnumerable<object> objects)
        {
            var result = new List<object>();
            using (var enumerator = objects.GetEnumerator())
            {
                var endOfArglist = !enumerator.MoveNext();
                object Peek() => enumerator.Current;

                object Get()
                {
                    var next = Peek();
                    endOfArglist = !enumerator.MoveNext();
                    return next;
                }

                while (!endOfArglist)
                {
                    if (Peek().Equals("\"") || Peek().Equals("\u201C"))  // Check for open quote
                    {
                        Get();  // Swallow quote
                        var tokens = new List<string>();
                        var notDone = true;
                        while (!endOfArglist && notDone)
                        {
                            var tok = Peek() as string;
                            switch (tok)
                            {
                                case null:
                                    throw new SyntaxError("Quoted strings may not contain subexpressions", SourceFile, lineNumber);

                                case "\"":
                                case "\u201D":  // Right double quote
                                    Get();  // Swallow quote
                                    notDone = false;
                                    result.Add(tokens.ToArray());
                                    break;

                                default:
                                    tokens.Add(tok);
                                    Get();
                                    break;
                            }
                        }
                        if (notDone)
                            throw new SyntaxError("Quoted strings missing close quote", SourceFile, lineNumber);
                    }
                    else 
                        result.Add(Canonicalize(Get()));
                }
            }

            return result.ToArray();
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
                    return StateVariableName.Named(s);

                switch (s)
                {
                    case "null":
                        return null;
                    case "empty":
                        return Cons.Empty;
                    case "true":
                        return true;
                    case "false":
                        return false;

                    case "=":
                        return StateVariableName.Named("=");
                    case ">":
                        return StateVariableName.Named(">");
                    case ">=":
                        return StateVariableName.Named(">=");
                    case "<":
                        return StateVariableName.Named("<");
                    case "<=":
                        return StateVariableName.Named("<=");
                }

                if (int.TryParse(s, out var result))
                    return result;
            }
            else if (o is object[] list)
                return CanonicalizeArglist(list);

            return o;
        }

        /// <summary>
        /// Read, parse, and return the information for all method definitions in the stream
        /// </summary>
        internal IEnumerable<(StateVariableName task, object[] pattern, LocalVariableName[] locals, Interpreter.Step chain, CompoundTask.TaskFlags flags,
                string path, int lineNumber)>
            Definitions
        {
            get
            {
                while (!end)
                    yield return ReadDefinition();
            }
        }

        private int lineNumber;

        
        private void InitParserState()
        {
            locals.Clear();
            referenceCounts.Clear();
            chainBuilder.Clear();
        }

        class ChainBuilder
        {
            public Interpreter.Step FirstStep;
            private Interpreter.Step previousStep;

            public void AddStep(Interpreter.Step s)
            {
                if (FirstStep == null)
                    FirstStep = previousStep = s;
                else
                {
                    // ReSharper disable once PossibleNullReferenceException
                    previousStep.Next = s;
                    previousStep = s;
                }
            }

            public void Clear()
            {
                FirstStep = previousStep = null;
            }
        }

        private readonly ChainBuilder chainBuilder = new ChainBuilder();

        /// <summary>
        /// Read and parse the next method definition
        /// </summary>
        private (StateVariableName task, object[] pattern, LocalVariableName[] locals, Interpreter.Step chain, CompoundTask.TaskFlags flags,
            string path, int lineNumber) ReadDefinition()
        {
            InitParserState();

            SwallowNewlines();
            var flags = ReadFlags();
            SwallowNewlines();

            lineNumber = expressionStream.LineNumber;

            var (taskName, pattern) = ReadHead();

            ReadBody(chainBuilder, () => EndOfDefinition);
            
            CheckForWarnings();

            // Eat end token
            if (EndOfDefinition && !end)
                Get(); // Skip over the delimiter
            SwallowNewlines();

            return (StateVariableName.Named(taskName), pattern.ToArray(), locals.ToArray(), chainBuilder.FirstStep, flags, expressionStream.FilePath, lineNumber);
        }

        private CompoundTask.TaskFlags ReadFlags()
        {
            void ThrowInvalid()
            {
                throw new SyntaxError($"Invalid task attribute", SourceFile, expressionStream.LineNumber);
            }

            var flags = CompoundTask.TaskFlags.None;

            while (Peek is object[] flagKeyword)
            {
                Get();
                if (flagKeyword.Length != 1 || !(flagKeyword[0] is string keyword)) ThrowInvalid();

                switch (keyword)
                {
                    // Shuffle rules when calling
                    case "randomly":
                        flags |= CompoundTask.TaskFlags.Shuffle;
                        break;

                    // Throw an error on total failure
                    case "generator":
                        flags |= CompoundTask.TaskFlags.Fallible | CompoundTask.TaskFlags.MultipleSolutions;
                        break;

                    // Limit it to first solution, as if the call were wrapped in Once.
                    case "fallible":
                        flags |= CompoundTask.TaskFlags.Fallible;
                        break;

                    case "retriable":
                        flags |= CompoundTask.TaskFlags.MultipleSolutions;
                        break;

                    default:
                        ThrowInvalid();
                        break;
                }
            }

            return flags;
        }

        /// <summary>
        /// Read the task name and argument pattern
        /// </summary>
        private (string taskName, List<object> pattern) ReadHead()
        {
            // Get the task name
            var taskName = Get() as string;
            if (taskName == null)
                throw new SyntaxError("Bracketed expression at start of definition", SourceFile, lineNumber);

            // Read the argument pattern
            var pattern = new List<object>();
            while (!Peek.Equals(":"))
            {
                var argPattern = Get();
                if (argPattern is object[] call)
                {
                    // It has an embedded predicate
                    pattern.Add(call[1]);
                    chainBuilder.AddStep(Call.MakeCall(Canonicalize(call[0]), Canonicalize(call[1]), null));
                }
                else
                    pattern.Add(argPattern);
            }

            // Change variable references in pattern to LocalVariableNames
            for (var i = 0; i < pattern.Count; i++)
                pattern[i] = Canonicalize(pattern[i]);

            // SKIP COLON
            Get();

            multiLine = EndOfLineToken;
            if (multiLine)
                Get();  // Swallow the end of line

            return (taskName, pattern);
        }

        private void ReadBody(ChainBuilder chain, Func<bool> endPredicate)
        {
            while (!endPredicate())
            {
                TryProcessTextBlock(chain);
                TryProcessMentionExpression(chain);
                TryProcessMethodCall(chain);
            }
        }
        
        /// <summary>
        /// If we're looking at a method call, compile it.
        /// </summary>
        private void TryProcessMethodCall(ChainBuilder chain)
        {
            if (AtKeywordMarker || !(Peek is object[] expression)) 
                return;

            // It's a call
            var targetName = expression[0] as string;
            switch (targetName)
            {
                case null:
                    throw new SyntaxError($"Invalid task name {expression[0]} in call.", SourceFile, lineNumber);

                case "s":
                case "es":
                    if (expression.Length != 1)
                        throw new ArgumentCountException("[s]", 0, expression.Skip(1).ToArray());
                    chain.AddStep(new ConjugateVerbStep(targetName, null));
                    break;

                case "add":
                    if (expression.Length != 3)
                        throw new ArgumentCountException("add", 2, expression.Skip(1).ToArray());
                    if (!(expression[2] is string vName && IsGlobalVariableName(vName)))
                        throw new SyntaxError($"Invalid global variable name in add: {expression[2]}", SourceFile, lineNumber);
                    chain.AddStep(new AddStep(Canonicalize(expression[1]), StateVariableName.Named(vName), null));
                    break;

                case "case":
                    if (expression.Length != 2)
                        throw new ArgumentCountException("case", 1, expression.Skip(1).ToArray());
                    chain.AddStep(ReadCase(Canonicalize(expression[1])));
                    break;

                case "cool":
                    if (expression.Length > 2)
                        throw new ArgumentCountException("cool", 1, expression.Skip(1).ToArray());

                    var duration = 1;
                    if (expression.Length == 2)
                    {
                        if (int.TryParse(expression[1] as string, out var d))
                            duration = d;
                        else
                            throw new SyntaxError($"Argument to cool must be an integer constant, but got {expression[1]}", SourceFile, lineNumber);
                    }

                    chain.AddStep(new CoolStep(duration, null));
                    break;

                case "once":
                    if (expression.Length != 1)
                        throw new ArgumentCountException("once", 0, expression.Skip(1).ToArray());

                    chain.AddStep(new CoolStep(int.MaxValue, null));
                    break;

                case "firstOf":
                case "randomly":
                case "sequence":
                    if (expression.Length != 1)
                        throw new ArgumentCountException(targetName, 1, expression.Skip(1).ToArray());
                    chain.AddStep(ReadAlternativeBranches(targetName));
                    break;

                case "set":
                    if (expression.Length != 3)
                        throw new ArgumentCountException("set", 2, expression.Skip(1).ToArray());
                    var name = expression[1] as string;
                    if (name == null || !IsGlobalVariableName(name))
                        throw new SyntaxError(
                            $"A Set command can only update a GlobalVariable; it can't update {expression[1]}", SourceFile, lineNumber);
                    chain.AddStep(new AssignmentStep(StateVariableName.Named(name), Canonicalize(expression[2]), null));
                    break;
                    
                default:
                    // This is a call
                    var target = IsLocalVariableName(targetName)
                        ? (object) GetLocal(targetName)
                        : StateVariableName.Named(targetName);
                    var args = CanonicalizeArglist(expression.Skip(1).Where(token => !token.Equals("\n")));
                    chain.AddStep(new Call(target, args, null));
                    break;
            }

            Get(); // Skip over the expression we just Peeked
        }

        /// <summary>
        /// Read the text of a branch of a [randomly] or [firstOf] expression
        /// </summary>
        /// <returns></returns>
        private Interpreter.Step ReadAlternativeBranches(string type)
        {
            var chains = new List<Interpreter.Step>();
            var chain = new ChainBuilder();
            while (!ExplicitEndToken)
            {
                Get(); // Skip keyword marker
                chain.Clear();
                ReadBody(chain, EndCaseBranch);
                chains.Add(chain.FirstStep);
            }

            if (type == "sequence")
                return new SequenceStep(chains.ToArray(), null);
            return new BranchStep(type, chains.ToArray(), null, type == "randomly");
        }

        /// <summary>
        /// Read a new CompoundTask that takes one argument, that corresponds to the body of an inline case expression.
        /// </summary>
        /// <returns></returns>
        private BranchStep ReadCase(object controlVar)
        {
            var chains = new List<Interpreter.Step>();
            var chain = new ChainBuilder();
            var arglist = new List<object>();
            while (!ExplicitEndToken)
            {
                chain.Clear();
                if (!ElseToken)
                {
                    Get(); // Skip keyword
                    arglist.Clear();
                    arglist.Add(controlVar);

                    var guard = Get();
                    while (guard.Equals("\n"))
                        guard = Get();
                    
                    if (guard is object[] expr)
                    {
                        guard = expr[0];
                        arglist.AddRange(expr.Skip(1));
                    }

                    var colon = Get();
                    if (!colon.Equals(":"))
                        throw new SyntaxError($"Unexpected token {colon} after test in case expression", SourceFile, lineNumber);
                    chain.AddStep(new Call(Canonicalize(guard), CanonicalizeArglist(arglist), null));
                }
                else 
                    Get(); // Skip keyword

                ReadBody(chain, EndCaseBranch);
                chains.Add(chain.FirstStep);
            }

            return new BranchStep("case", chains.ToArray(), null, false);
        }

        /// <summary>
        /// If we're looking at a mention expression (?x, ?x/Foo, ?x/Foo/Bar, etc.), compile it.
        /// </summary>
        private void TryProcessMentionExpression(ChainBuilder chain)
        {
            if (EndOfDefinition || !IsLocalVariableName(Peek))
                return;

            var local = GetLocal((string) Get());

            if (!Peek.Equals("/"))
            {
                // This is a simple variable mention
                chain.AddStep(Call.MakeCall(Call.MentionHook, local, null));
                return;
            }

            ReadComplexMentionExpression(chain, local);
        }

        /// <summary>
        /// Read an expression of the form ?local/STUFF
        /// Called after ?local has already been read.
        /// </summary>
        /// <param name="chain">Chain to add to</param>
        /// <param name="local">The variable before the /</param>
        private void ReadComplexMentionExpression(ChainBuilder chain, LocalVariableName local)
        {
// This is a complex "/" expression
            while (Peek.Equals("/"))
            {
                Get(); // Swallow slash
                var t = Get();
                if (!(t is string targetName))
                    throw new SyntaxError($"Invalid method name after the /: {local}/{t}", SourceFile, lineNumber);
                var target = IsLocalVariableName(targetName)
                    ? (object) GetLocal(targetName)
                    : StateVariableName.Named(targetName);
                if (Peek.Equals("/"))
                {
                    var tempVar = GetFreshLocal("temp");
                    // There's another / coming so this is a function call
                    chain.AddStep(Call.MakeCall(target, local, tempVar, null));
                    local = tempVar;
                }
                else
                {
                    ReadMentionExpressionTail(chain, local, target);
                }
            }
        }

        /// <summary>
        /// Called after the last "/" of a complex mention expression.
        /// </summary>
        /// <param name="chain">Chain to add to</param>
        /// <param name="local">Result of the expression from before the last "/"</param>
        /// <param name="targetVar">Task to call on local</param>
        private void ReadMentionExpressionTail(ChainBuilder chain, LocalVariableName local, object targetVar)
        {
            AddMentionExpressionTail(chain, targetVar, local);
            while (Peek.Equals("+"))
            {
                Get(); // Swallow plus
                var targetToken = Get();
                if (!(targetToken is string targetName))
                    throw new SyntaxError($"Invalid method name after the /: {local}/{targetToken}", SourceFile, lineNumber);
                var target = IsLocalVariableName(targetName)
                    ? (object) GetLocal(targetName)
                    : StateVariableName.Named(targetName);
                AddMentionExpressionTail(chain, target, local);
            }
        }

        static readonly StateVariableName BinaryTask = StateVariableName.Named("BinaryTask");
        private void AddMentionExpressionTail(ChainBuilder chain, object target, LocalVariableName local)
        {
            var temp = GetFreshLocal("temp");
            chain.AddStep(new BranchStep(target.ToString(),
                new Interpreter.Step[]{ 
                    Call.MakeCall(BinaryTask, target,
                    Call.MakeCall(target, local, temp, 
                        Call.MakeCall(Call.MentionHook, temp, null))),
                    Call.MakeCall(target, local, null)},
                null,
                false));
        }

        /// <summary>
        /// If this is a sequence of fixed text tokens, compile them into an EmitStep.
        /// </summary>
        private void TryProcessTextBlock(ChainBuilder chain)
        {
            tokensToEmit.Clear();
            while (!EndOfDefinition && Peek is string && !IsNonAnonymousLocalVariableName(Peek))
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
                chain.AddStep(new EmitStep(tokensToEmit.ToArray(), null));
        }

        /// <summary>
        /// Issue warnings for any singleton variables
        /// </summary>
        private void CheckForWarnings()
        {
            for (var i = 0; i < locals.Count; i++)
                if (referenceCounts[i] == 1 && !locals[i].Name.StartsWith("?"))
                    Module.AddWarning(
                        $"{SourceFile}:{lineNumber} Singleton variable {locals[i].Name}");
        }
    }
}
