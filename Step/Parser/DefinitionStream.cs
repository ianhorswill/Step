﻿#region Copyright
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
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Channels;
using Step.Interpreter;
using Step.Utilities;

namespace Step.Parser
{
    /// <summary>
    /// Reads a stream of method definitions from a TextReader.
    /// </summary>
    internal class DefinitionStream : IDisposable
    {
        public DefinitionStream(Module module, string filePath) : this(new ExpressionStream(filePath), module)
        { }
        
        /// <summary>
        /// Reads definitions from the specified stream
        /// </summary>
        public DefinitionStream(TextReader stream, Module module, string filePath)
            : this(new ExpressionStream(stream, filePath), module)
        {

        }

        public void Dispose()
        {
            expressionStream.Dispose();
        }

        ///
        /// Make a new definition stream
        /// 
        public DefinitionStream(ExpressionStream expressions, Module module)
        {
            Module = module;
            expressionStream = expressions;
            this.expressions = expressions.Expressions.GetEnumerator();
            MoveNext();
            chainBuilder = new Interpreter.Step.ChainBuilder(GetLocal, Canonicalize, CanonicalizeArglist);
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
        public static bool IsLocalVariableName(object token) => token is string s && s.StartsWith("?");

        /// <summary>
        /// The token is a valid name of a non-anonymous local variable
        /// </summary>
        public static bool IsNonAnonymousLocalVariableName(object token) => token is string s && s.StartsWith("?") && s.Any(char.IsLetter);

        /// <summary>
        /// The local variable name indicates the programmer intended it to be a singleton.
        /// </summary>
        public static bool IsSingletonVariableName(string name) => name == "?" || name.StartsWith("?_");

        /// <summary>
        /// The local variable name indicates the programmer intended it to be a singleton.
        /// </summary>
        public static bool IsIntendedAsSingleton(LocalVariableName l) => IsSingletonVariableName(l.Name);

        /// <summary>
        /// True if the string is a valid global variable name
        /// </summary>
        public static bool IsGlobalVariableName(object token) => token is string s && char.IsUpper(s[0]);

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

                // ReSharper disable once LocalFunctionHidesMethod
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
            switch (o)
            {
                case string s when IsLocalVariableName(s):
                {
                    var v = GetLocal(s);
                    IncrementReferenceCount(v);
                    return v;
                }
                case string s when IsGlobalVariableName(s):
                    return StateVariableName.Named(s);
                case string s:
                {
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
                    if (float.TryParse(s, out var fResult))
                        return fResult;
                    break;
                }
                
                case string[] text:
                    return text;
                case object[] list:
                    return CanonicalizeArglist(list);
                case TupleExpression t:
                    return CanonicalizeArglist(t.Elements);
            }

            return o;
        }

        private void IncrementReferenceCount(LocalVariableName v)
        {
            referenceCounts[v.Index] += 1;
        }

        /// <summary>
        /// Read, parse, and return the information for all method definitions in the stream
        /// </summary>
        internal IEnumerable<(StateVariableName task, float weight, object[] pattern,
                LocalVariableName[] locals, 
                Interpreter.Step chain, 
                CompoundTask.TaskFlags flags,
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

        private readonly Interpreter.Step.ChainBuilder chainBuilder;

        /// <summary>
        /// Read and parse the next method definition
        /// </summary>
        private (StateVariableName task, float weight, object[] pattern,
            LocalVariableName[] locals, 
            Interpreter.Step chain, 
            CompoundTask.TaskFlags flags,
                string path, int lineNumber) ReadDefinition()
        {
            InitParserState();

            SwallowNewlines();

            lineNumber = expressionStream.LineNumber;

            var (flags, weight) = ReadOptions();
            SwallowNewlines();

            lineNumber = expressionStream.LineNumber;

            if (Peek.Equals("predicate") || Peek.Equals("task") || Peek.Equals("fluent"))
                return ReadDeclaration(flags);
            
            var (taskName, pattern) = ReadHead();

            ReadBody(chainBuilder, () => EndOfDefinition);
            
            CheckForWarnings();

            // Eat end token
            if (EndOfDefinition && !end)
                Get(); // Skip over the delimiter
            SwallowNewlines();

            return (StateVariableName.Named(taskName), weight, pattern.ToArray(), locals.ToArray(), chainBuilder.FirstStep, flags, expressionStream.FilePath, lineNumber);
        }

        private (StateVariableName task, float weight, object[] pattern, 
            LocalVariableName[] locals,
            Interpreter.Step chain,
            CompoundTask.TaskFlags flags, 
            string path, int lineNumber)
            ReadDeclaration(CompoundTask.TaskFlags flags)
        {
            var declType = Get(); // swallow "task" or "predicate

            if (declType.Equals("predicate")  || declType.Equals("fluent"))
                flags |= CompoundTask.TaskFlags.Fallible | CompoundTask.TaskFlags.MultipleSolutions;
            if (declType.Equals("fluent"))
                flags |= CompoundTask.TaskFlags.ReadCache;

            var (taskName, pattern) = ReadHead();

            if (multiLine)
                throw new SyntaxError(
                    $"{declType} declarations must end with a period after the name of this task, but this declaration ends with a colon.",
                    SourceFile, lineNumber);
            
            SwallowNewlines();
            
            return (StateVariableName.Named(taskName), 0, pattern.ToArray(), null, null, flags, SourceFile, lineNumber);
        }

        private (CompoundTask.TaskFlags, float weight) ReadOptions()
        {
            var weight = 1f;
            void ThrowInvalid(object[] attr)
            {
                throw new SyntaxError($"Invalid task attribute {Writer.TermToString(attr)}", SourceFile, expressionStream.LineNumber);
            }

            var flags = CompoundTask.TaskFlags.None;

            while (Peek is object[] optionKeyword)
            {
                Get();
                string keyword = null;
                if (optionKeyword.Length == 1 && optionKeyword[0] is string op0)
                    keyword = op0;
                else
                    ThrowInvalid(optionKeyword);

                switch (keyword)
                {
                    // Shuffle rules when calling
                    case "randomly":
                        flags |= CompoundTask.TaskFlags.Shuffle;
                        break;

                    case "remembered":
                        flags |= CompoundTask.TaskFlags.ReadCache | CompoundTask.TaskFlags.WriteCache;
                        break;

                    case "fluent":
                        flags |= CompoundTask.TaskFlags.Fallible | CompoundTask.TaskFlags.MultipleSolutions | CompoundTask.TaskFlags.ReadCache;
                        break;

                    case "function":
                        flags |= CompoundTask.TaskFlags.Function;
                        break;

                    case "generator":
                    case "predicate":
                        flags |= CompoundTask.TaskFlags.Fallible | CompoundTask.TaskFlags.MultipleSolutions;
                        break;

                    // Limit it to first solution, as if the call were wrapped in Once.
                    case "fallible":
                        flags |= CompoundTask.TaskFlags.Fallible;
                        break;

                    case "retriable":
                        flags |= CompoundTask.TaskFlags.MultipleSolutions;
                        break;

                    case "main":
                        flags |= CompoundTask.TaskFlags.Main;
                        break;

                    case "suffix":
                        flags |= CompoundTask.TaskFlags.Suffix;
                        break;

                    default:
                        if (!float.TryParse(keyword, out weight))
                            ThrowInvalid(optionKeyword);
                        break;
                }

                SwallowNewlines();
            }

            return (flags, weight);
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
            while (!Peek.Equals(":") && !Peek.Equals(".") && !end)
            {
                var argPattern = Get();
                switch (argPattern)
                {
                    case string quote when quote == "“":
                        var strings = new List<string>();
                        while (!Peek.Equals("”") && !end)
                        {
                            var next = Get();
                            if (next is string s)
                                strings.Add(s);
                            else 
                                throw new SyntaxError("Bracketed expressions are not allowed inside string constants", SourceFile, lineNumber);
                        }

                        if (end)
                            throw new SyntaxError("Unexpected end of file inside a quoted string", SourceFile,
                                lineNumber);
                        else
                            // Skip close quote
                            Get();
                        pattern.Add(strings.ToArray());
                        break;
                    
                    case TupleExpression call when call.BracketStyle == "()":
                        LocalVariableName argument = null;
                        var callCopy = (object[])(call.Elements.Clone());
                        for (int i = 0; i < callCopy.Length; i++)
                        {
                            var element = callCopy[i];
                            if (IsLocalVariableName(element) && !pattern.Any(arg => arg is LocalVariableName n && n.Name.Equals(element)))
                            {
                                var local = GetLocal((string) element);
                                
                                // Found a new local variable name
                                if (argument != null)
                                {
                                    throw new SyntaxError(
                                        $"Ambiguous guard expression: can't tell if {argument} or {local} is the argument in {call}",
                                        SourceFile, lineNumber);
                                }

                                argument = local;
                                // We do this in case the local variable name is "?"
                                callCopy[i] = local;
                                IncrementReferenceCount(local);   // Ugly...
                            }
                        }

                        if (argument == null)
                            throw new SyntaxError($"Invalid guard expression {call} contains no new variables",
                                SourceFile, lineNumber);
                        // It has an embedded predicate
                        pattern.Add(argument);
                        chainBuilder.AddStep(new Call(Canonicalize(callCopy[0]), callCopy.Skip(1).Select(Canonicalize).ToArray(), null));
                        break;
                    
                    default:
                        pattern.Add(argPattern);
                        break;
                }
            }

            if (end)
                throw new SyntaxError("Head of method does not end with a colon", SourceFile, lineNumber);

            // Change variable references in pattern to LocalVariableNames
            for (var i = 0; i < pattern.Count; i++)
                pattern[i] = Canonicalize(pattern[i]);

            var headTerminator = Peek;
            // SKIP COLON
            Get();

            multiLine = EndOfLineToken && !headTerminator.Equals(".");
            if (multiLine)
                Get();  // Swallow the end of line

            return (taskName, pattern);
        }

        private void ReadBody(Interpreter.Step.ChainBuilder chain, Func<bool> endPredicate)
        {
            while (!endPredicate())
            {
                if (end)
                    throw new SyntaxError("File ended unexpectedly inside method", SourceFile, lineNumber);
                TryProcessTextBlock(chain);
                TryProcessMentionExpression(chain);
                TryProcessMethodCall(chain);
            }
        }

        /// <summary>
        /// If we're looking at a method call, compile it.
        /// </summary>
        private void TryProcessMethodCall(Interpreter.Step.ChainBuilder chain)
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
                    AddStep.FromExpression(chain, expression, SourceFile, lineNumber);
                    break;

                case "removeNext":
                    RemoveNextStep.FromExpression(chain, expression, SourceFile, lineNumber);
                    break;

                case "case":
                    if (expression.Length != 2)
                        throw new ArgumentCountException("case", 1, expression.Skip(1).ToArray());
                    chain.AddStep(ReadCase(Canonicalize(expression[1])));
                    break;

                case "cool":
                    CoolStep.FromCoolExpression(chain, expression, SourceFile, lineNumber);
                    break;

                case "now":
                    FluentUpdateStep.FromExpression(chain, expression, Module, SourceFile, lineNumber);
                    break;

                case "once":
                    CoolStep.FromOnceExpression(chain, expression);
                    break;

                case "firstOf":
                case "randomly":
                case "sequence":
                    if (expression.Length != 1)
                        throw new ArgumentCountException(targetName, 1, expression.Skip(1).ToArray());
                    chain.AddStep(ReadAlternativeBranches(targetName));
                    break;

                case "set":
                    AssignmentStep.FromExpression(chain, expression, SourceFile, lineNumber);
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
            var chain = new Interpreter.Step.ChainBuilder(GetLocal, Canonicalize, CanonicalizeArglist);
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
            var chain = new Interpreter.Step.ChainBuilder(GetLocal, Canonicalize, CanonicalizeArglist);
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
        private void TryProcessMentionExpression(Interpreter.Step.ChainBuilder chain)
        {
            if (EndOfDefinition || !IsLocalVariableName(Peek))
                return;

            var local = GetLocal((string) Get());
            IncrementReferenceCount(local);

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
        private void ReadComplexMentionExpression(Interpreter.Step.ChainBuilder chain, LocalVariableName local)
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
        private void ReadMentionExpressionTail(Interpreter.Step.ChainBuilder chain, LocalVariableName local, object targetVar)
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
        private void AddMentionExpressionTail(Interpreter.Step.ChainBuilder chain, object target, LocalVariableName local)
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
        private void TryProcessTextBlock(Interpreter.Step.ChainBuilder chain)
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
                if (referenceCounts[i] == 1 && !IsIntendedAsSingleton(locals[i]))
                    Module.AddWarning(
                        $"{SourceFile}:{lineNumber} Variable {locals[i].Name} used only once, which often means it's a type-o.  If it's deliberate, change the name to {locals[i].Name.Replace("?", "?_")} to suppress this message.\n");
        }
    }
}
