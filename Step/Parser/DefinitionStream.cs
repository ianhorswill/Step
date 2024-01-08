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
using Step.Output;

namespace Step.Parser
{
    /// <summary>
    /// Reads a stream of method definitions from a TextReader.
    /// </summary>
    internal class DefinitionStream : IDisposable
    {
        static DefinitionStream()
        {
            // This is just to force TextUtilities' static constructor to run
            // That ensures its control tokens are defined before we try to parse anything.
            TextUtilities.Initialize();
        }

        private static readonly Dictionary<string, object> Substitutions = new Dictionary<string, object>();

        /// <summary>
        /// Defines that any occurrence of the single-element bracketed expression [macro] in the input text should be replaced by substitution.
        /// </summary>
        /// <param name="macro"></param>
        /// <param name="substitution"></param>
        public static void DefineSubstitution(string macro, object substitution) =>
            Substitutions[macro] = substitution;

        public DefinitionStream(Module module, string filePath) : this(new ExpressionStream(filePath), module)
        { }
        
        /// <summary>
        /// Reads definitions from the specified stream
        /// </summary>
        public DefinitionStream(TextReader stream, Module module, string? filePath)
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
        public DefinitionStream(ExpressionStream expressionStream, Module module)
        {
            Module = module;
            this.expressionStream = expressionStream;
            this.expressions = expressionStream.Expressions.GetEnumerator();
            MoveNext();
            chainBuilder = new Interpreter.Step.ChainBuilder(GetLocal, Canonicalize, CanonicalizeArglist);
            groupExpander = new DeclarationGroupExpander(module);
        }

        /// <summary>
        /// Module into which this is reading definitions
        /// </summary>
        public readonly Module Module;

        #region Stream interface

        private readonly ExpressionStream expressionStream;

        private readonly DeclarationGroupExpander groupExpander;

        public string? SourceFile
        {
            get
            {
                var path = expressionStream.FilePath;
                return path == null ? "Unknown" : Path.GetFileName(path);
            }
        }

        public string? SourcePath => expressionStream.FilePath;

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
        private object? Peek => expressions.Current;

        /// <summary>
        /// Peek, but apply substitution
        /// </summary>
        private object? PeekAndSubstitute
        {
            get
            {
                var next = Peek;
                if (next is object[] { Length: 1 } a && a[0] is string keyword &&
                    Substitutions.TryGetValue(keyword, out var subst))
                    return subst;
                return next;
            }
        }
        
        /// <summary>
        /// Return the current expression and move to the next
        /// </summary>
        /// <returns></returns>
        private object? Get()
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
            while (!end && AtEndOfLine)
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
        private bool EndOfDefinition => end || ExplicitEndToken || (!multiLine && AtEndOfLine);

        private bool AtEndOfLine => EndOfLineToken || EndOfParagraphToken;
        
        /// <summary>
        /// True if we're at an end of line token
        /// </summary>
        private bool EndOfLineToken => Equals(Peek, EndOfLine);

        private bool EndOfParagraphToken => ReferenceEquals(Peek, TextUtilities.NewParagraphToken);

        /// <summary>
        /// True if we're at an "[end]" expression
        /// </summary>
        private bool ExplicitEndToken => KeywordMarker("end");

        private bool KeywordMarker(string keyword) =>
            Peek is object[] { Length: 1 } array && array[0].Equals(keyword);

        private bool OneOfKeywordMarkers(params string[] keywords) =>
            Peek is object[] { Length: 1 } array && array[0] is string k && Array.IndexOf(keywords, k) >= 0;

        private readonly string[] keywordMarkers = new[] {"end", "or", "else", "then" };
        private bool AtKeywordMarker => OneOfKeywordMarkers(keywordMarkers);

        private readonly string[] caseBranchKeywords = new[] {"end", "or", "else", "then" };
        private bool EndCaseBranch() => OneOfKeywordMarkers(caseBranchKeywords);

        private bool ElseToken => KeywordMarker("else");

        private static readonly string[] DeclarationKeywords = { "predicate", "task", "fluent", "folder_structure" };
        #endregion

        #region Source-language variables     
        /// <summary>
        /// True if the string is a valid local variable name
        /// </summary>
        public static bool IsLocalVariableName(object? token) => token is string s && s.StartsWith("?");

        /// <summary>
        /// The token is a valid name of a non-anonymous local variable
        /// </summary>
        public static bool IsNonAnonymousLocalVariableName(object? token) => token is string s && s.StartsWith("?") && s.Any(char.IsLetter);

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
        public static bool IsGlobalVariableName(object token) 
            => token is string s 
               && s.Length>0 
               && (char.IsUpper(s[0]) || IsUpArrowGlobalVariableReference(s));

        private static bool IsUpArrowGlobalVariableReference(string s)
        {
            return s.Length > 1 && s[0] == '^' && char.IsUpper(s[1]);
        }

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

        private static readonly string[] QuoteTokens = { "\"", "\u201C", "\u201D" };

        /// <summary>
        /// Identify tokens that identify non-strings (variables, numbers) and replace them
        /// with their internal representations
        /// </summary>
        object?[] CanonicalizeArglist(IEnumerable<object?> objects)
        {
            var result = new List<object?>();
            using (var enumerator = objects.GetEnumerator())
            {
                var endOfArglist = !enumerator.MoveNext();
                object? Peek() => enumerator.Current;

                // ReSharper disable once LocalFunctionHidesMethod
                object? Get()
                {
                    var next = Peek();
                    endOfArglist = !enumerator.MoveNext();
                    return next;
                }

                while (!endOfArglist)
                {
                    if (QuoteTokens.Contains(Peek()))  // Check for open quote
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
                                    throw new SyntaxError("Quoted text may not contain subexpressions", SourceFile, lineNumber);

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
                            throw new SyntaxError("Quoted text missing close quote", SourceFile, lineNumber);
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
        object? Canonicalize(object? o)
        {
            switch (o)
            {
                case string s when TextFileTokenStream.IsEscapedStringToken(s):
                    return TextFileTokenStream.UnescapeStringToken(s);

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
        internal IEnumerable<(StateVariableName task, float weight, object?[] pattern,
                LocalVariableName[]? locals, 
                Interpreter.Step? chain, 
                CompoundTask.TaskFlags flags,
                string? declaration,
                string? path, int lineNumber)>
            Definitions
        {
            get
            {
                while (!end)
                {
                    yield return ReadDefinition();
                    while (groupExpander.Assertions.Count > 0)
                    {
                        var assertionLocals = new List<LocalVariableName>();

                        LocalVariableName MakeLocal(string name)
                        {
                            // ReSharper disable once RedundantSuppressNullableWarningExpression
                            var l = new LocalVariableName(name, assertionLocals!.Count);
                            assertionLocals.Add(l);
                            return l;
                        }

                        LocalVariableName Localize(string name) =>
                            assertionLocals.FirstOrDefault(l => l.Name == name) ?? MakeLocal(name);

                        object? Variablize(object? o) => o switch
                        {
                            null => null,
                            string s when IsLocalVariableName(s) => Localize(s),
                            object?[] tuple => VariablizeTuple(tuple),
                            _ => o
                        };

                        object?[] VariablizeTuple(object?[] tuple) => tuple.Select(Variablize).ToArray();

                        var assertion = groupExpander.Assertions.Dequeue();
                        var pattern = VariablizeTuple(assertion.pattern);
                        yield return (assertion.task, 1, pattern, assertionLocals.ToArray(), null, CompoundTask.TaskFlags.None,
                            null, SourcePath, lineNumber);
                    }
                }
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
        private (StateVariableName task, float weight, object?[] pattern,
            LocalVariableName[]? locals, 
            Interpreter.Step? chain, 
            CompoundTask.TaskFlags flags,
            string? declaration,
                string? path, int lineNumber)
            ReadDefinition()
        {
            InitParserState();

            SwallowNewlines();

            lineNumber = expressionStream.LineNumber;

            var (flags, weight) = ReadOptions();
            SwallowNewlines();

            lineNumber = expressionStream.LineNumber;

            if (DeclarationKeywords.Contains(Peek))
                return ReadDeclaration(flags);
            
            var (taskName, pattern) = groupExpander.ExpandHead(ReadHead(), GetLocal, SourcePath, lineNumber);

            ReadBody(chainBuilder, () => EndOfDefinition);
            
            CheckForWarnings(taskName, SourcePath, lineNumber);

            // Eat end token
            if (EndOfDefinition && !end)
                Get(); // Skip over the delimiter
            SwallowNewlines();

            return (StateVariableName.Named(taskName), weight, pattern, locals.ToArray(), chainBuilder.FirstStep, flags, null, expressionStream.FilePath, lineNumber);
        }

        private (StateVariableName task, float weight, object?[] pattern, 
            LocalVariableName[]? locals,
            Interpreter.Step? chain,
            CompoundTask.TaskFlags flags,
            string? declaration,
            string? path, int lineNumber)
            ReadDeclaration(CompoundTask.TaskFlags flags)
        {
            var declType = (string?)Get(); // swallow "task" or "predicate

            if (declType == "predicate"  || declType == "fluent" || declType == "folder_structure")
                flags |= CompoundTask.TaskFlags.Fallible | CompoundTask.TaskFlags.MultipleSolutions;
            if (Equals(declType, "fluent"))
                flags |= CompoundTask.TaskFlags.ReadCache;

            var (taskName, pattern) = ReadHead();

            if (multiLine)
                throw new SyntaxError(
                    $"{declType} declarations must end with a period after the name of this task, but this declaration ends with a colon.",
                    SourceFile, lineNumber);
            
            SwallowNewlines();
            
            return (StateVariableName.Named(taskName), 0, pattern.ToArray(), null, null, flags, declType, SourceFile, lineNumber);
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
                if (!groupExpander.HandleGroupAttribute(optionKeyword, SourcePath, lineNumber))
                {
                    string? keyword = null;
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
                            flags |= CompoundTask.TaskFlags.Fallible | CompoundTask.TaskFlags.MultipleSolutions |
                                     CompoundTask.TaskFlags.ReadCache;
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
                }

                SwallowNewlines();
            }

            return (flags, weight);
        }

        /// <summary>
        /// Read the task name and argument pattern
        /// </summary>
        private (string taskName, List<object?> pattern) ReadHead()
        {
            // Get the task name
            var taskName = Get() as string;
            if (taskName == null)
                throw new SyntaxError("Bracketed expression at start of definition", SourceFile, lineNumber);

            // Read the argument pattern
            var pattern = new List<object?>();
            while (!Equals(Peek, ":") && !Equals(Peek, ".") && !end)
            {
                var argPattern = Get();
                switch (argPattern)
                {
                    case string quote when quote == "“":
                        var strings = new List<string>();
                        while (!Equals(Peek, "”") && !end)
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
                    
                    case string paren when paren == "(":
                        var guardElements = new List<object?>();
                        while (!Equals(Peek, ")") && !end) guardElements.Add(Get());
                        if (end)
                            throw new SyntaxError("Method head ended in the middle of a ( ... ) expression.",
                                SourcePath, lineNumber);
                        else 
                            Get();  // swallow )

                        LocalVariableName? argument = null;
                        var callCopy = guardElements.ToArray();
                        for (int i = 0; i < callCopy.Length; i++)
                        {
                            var element = callCopy[i];
                            if (IsLocalVariableName(element) && !pattern.Any(arg => arg is LocalVariableName n && n.Name.Equals(element)))
                            {
                                var local = GetLocal((string) element!);
                                
                                // Found a new local variable name
                                if (argument != null)
                                {
                                    throw new SyntaxError(
                                        $"Ambiguous guard expression: can't tell if {argument} or {local} is the argument in () expression",
                                        SourceFile, lineNumber);
                                }

                                argument = local;
                                // We do this in case the local variable name is "?"
                                callCopy[i] = local;
                                IncrementReferenceCount(local);   // Ugly...
                            }
                        }

                        if (argument == null)
                            throw new SyntaxError($"Invalid guard expression {new TupleExpression("()", guardElements.ToArray())} contains no new variables",
                                SourceFile, lineNumber);
                        // It has an embedded predicate
                        pattern.Add(argument);
                        chainBuilder.AddStep(new Call(Canonicalize(callCopy[0])!, callCopy.Skip(1).Select(Canonicalize).ToArray(), null));
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

            multiLine = AtEndOfLine && !Equals(headTerminator, ".");
            if (multiLine && !EndOfParagraphToken)
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

            if (expression.Length == 0)
                throw new SyntaxError("The expression '[]' appears to be intended as a method call, but there is no task given inside of it.", SourceFile, lineNumber);
            
            // It's a call
            var targetName = expression[0] as string;
            switch (targetName)
            {
                case null:
                    throw new SyntaxError($"Invalid task name {expression[0]} in call.", SourceFile, lineNumber);

#if ConjugateVerbStep
                case "s":
                case "es":
                    if (expression.Length != 1)
                        throw new ArgumentCountException("[s]", 0, expression.Skip(1).ToArray());
                    chain.AddStep(new ConjugateVerbStep(targetName, null));
                    break;
#endif

                case "add":
                    AddStep.FromExpression(chain, expression, SourceFile, lineNumber);
                    break;

                case "removeNext":
                    RemoveNextStep.FromExpression(chain, expression, SourceFile, lineNumber);
                    break;

                case "case":
                    if (expression.Length != 2)
                        throw new ArgumentCountException("case", 1, expression.Skip(1).ToArray());
                    var controlVar = expression[1];
                    if (controlVar == null)
                        throw new SyntaxError($"null cannot be used as the control variable for a case statement",
                            SourceFile, lineNumber);
                    chain.AddStep(ReadCase(Canonicalize(controlVar)!));
                    break;

                case "cool":
                    CoolStep.FromCoolExpression(chain, expression, SourceFile, lineNumber);
                    break;

                case "now":
                    if (expression.Length >= 3 && expression[2].Equals("="))
                        AssignmentStep.FromExpression(chain, expression, SourceFile, lineNumber);
                    else
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
                case "inc":
                case "dec":
                    AssignmentStep.FromExpression(chain, expression, SourceFile, lineNumber);
                    break;

                default:
                    // This is a call
                    LocalVariableName? local = null;
                    var isLocal = IsLocalVariableName(targetName);
                    if (isLocal)
                    {
                        local = GetLocal(targetName);
                        IncrementReferenceCount(local);
                    }
                    var target = isLocal ? (object?) local : StateVariableName.Named(targetName);
                    var args = CanonicalizeArglist(expression.Skip(1).Where(token => !token.Equals("\n")));
                    chain.AddStep(new Call(target!, args, null));
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
            var chains = new List<Interpreter.Step?>();
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
            var chains = new List<Interpreter.Step?>();
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
                    while (Equals(guard, "\n"))
                        guard = Get();
                    
                    if (guard is object[] expr)
                    {
                        guard = expr[0];
                        arglist.AddRange(expr.Skip(1));
                    }

                    var colon = Get();
                    if (!Equals(colon,":"))
                        throw new SyntaxError($"Unexpected token {colon} after test in case expression", SourceFile, lineNumber);
                    chain.AddStep(new Call(Canonicalize(guard)!, CanonicalizeArglist(arglist), null));
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
            if (EndOfDefinition)
                return;

            var peek = Peek as string;

            if (string.IsNullOrEmpty(peek))
                return;

            IVariableName variable;

            if (IsLocalVariableName(peek))
            {
                var local = GetLocal(peek);
                IncrementReferenceCount(local);
                variable = local;
            } else if (IsUpArrowGlobalVariableReference(peek))
                variable = StateVariableName.Named(peek);
            else 
                return;

            Get();

            if (!Equals(Peek, "/"))
            {
                // This is a simple variable mention
                chain.AddStep(Call.MakeCall(Call.MentionHook, variable, null));
                return;
            }

            ReadComplexMentionExpression(chain, variable);
        }

        /// <summary>
        /// Read an expression of the form ?local/STUFF
        /// Called after ?local has already been read.
        /// </summary>
        /// <param name="chain">Chain to add to</param>
        /// <param name="variable">The variable before the /</param>
        private void ReadComplexMentionExpression(Interpreter.Step.ChainBuilder chain, IVariableName variable)
        {
// This is a complex "/" expression
            while (Equals(Peek, "/"))
            {
                Get(); // Swallow slash
                var t = Get();
                if (!(t is string targetName))
                    throw new SyntaxError($"Invalid method name after the /: {variable}/{t}", SourceFile, lineNumber);

                var target = IsLocalVariableName(targetName)
                    ? (object) GetLocal(targetName)
                    : StateVariableName.Named(targetName);
                if (target is LocalVariableName local)
                    IncrementReferenceCount(local);

                if (Peek.Equals("/"))
                {
                    var tempVar = GetFreshLocal("temp");
                    // There's another / coming so this is a function call
                    chain.AddStep(Call.MakeCall(target, variable, tempVar, null));
                    variable = tempVar;
                }
                else
                {
                    ReadMentionExpressionTail(chain, variable, target);
                }
            }
        }

        /// <summary>
        /// Called after the last "/" of a complex mention expression.
        /// </summary>
        /// <param name="chain">Chain to add to</param>
        /// <param name="variable">Result of the expression from before the last "/"</param>
        /// <param name="targetVar">Task to call on local</param>
        private void ReadMentionExpressionTail(Interpreter.Step.ChainBuilder chain, IVariableName variable, object targetVar)
        {
            AddMentionExpressionTail(chain, targetVar, variable);
            while (Equals(Peek, "+"))
            {
                Get(); // Swallow plus
                var targetToken = Get();
                if (!(targetToken is string targetName))
                    throw new SyntaxError($"Invalid method name after the /: {variable}/{targetToken}", SourceFile, lineNumber);
                var target = IsLocalVariableName(targetName)
                    ? (object) GetLocal(targetName)
                    : StateVariableName.Named(targetName);
                AddMentionExpressionTail(chain, target, variable);
            }
        }

        static readonly StateVariableName BinaryTask = StateVariableName.Named("BinaryTask");
        private void AddMentionExpressionTail(Interpreter.Step.ChainBuilder chain, object target, IVariableName local)
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
            if (Peek is TupleExpression)
                throw new SyntaxError(
                    "Parenthesized expression is invalid in a method body.  Use \\( if you mean to include a parenthesis in the text",
                    SourcePath, lineNumber);
            while (!EndOfDefinition
                   && PeekAndSubstitute is string s
                   && !(IsNonAnonymousLocalVariableName(s) || IsUpArrowGlobalVariableReference(s)))
            {
                if (!EndOfLineToken)
                    tokensToEmit.Add(s);
                Get();
            }

            if (tokensToEmit.Count > 0)
                chain.AddStep(new EmitStep(tokensToEmit.ToArray(), null));
        }

        /// <summary>
        /// Issue warnings for any singleton variables
        /// </summary>
        private void CheckForWarnings(string taskName, string? sourcePath, int warningLine)
        {
            for (var i = 0; i < locals.Count; i++)
                if (referenceCounts[i] == 1 && !IsIntendedAsSingleton(locals[i]) && !groupExpander.IsVariableFromCurrentDeclarationGroup(locals[i]))
                    Module.AddWarning(
                        $"{SourceFile}:{warningLine} Variable {locals[i].Name} used only once, which often means it's a type-o.  If it's deliberate, change the name to {locals[i].Name.Replace("?", "?_")} to suppress this message.",
                        new MethodPlaceholder(taskName, sourcePath, warningLine));
        }
    }
}
