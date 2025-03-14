﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Module.cs" company="Ian Horswill">
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Step.Interpreter;
using Step.Output;
using Step.Parser;
using Step.Utilities;
using Task = Step.Interpreter.Task;

[assembly: InternalsVisibleTo("Tests")]

namespace Step
{
    /// <summary>
    /// Stores values of global state variables
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class Module
    {
        /// <summary>
        /// Stack traces should be generated with Unity rich text markup
        /// </summary>
        public static bool RichTextStackTraces;

        public static int DefaultSearchLimit = 10000;

        /// <summary>
        /// Number of steps system is allowed to execute before being interrupted.
        /// </summary>
        public static int SearchLimit;

        /// <summary>
        /// True if we are in the process of trying to cancel the running Step thread.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private static bool isCanceling;

        /// <summary>
        /// Force whatever worker thread is running Step code to stop and throw a TaskCanceledException.
        /// </summary>
        public static void Cancel()
        {
            isCanceling = true;
            SearchLimit = 1;
        }

        /// <summary>
        /// Reset control variables to let the task run again.
        /// </summary>
        /// <param name="searchLimit">Value to set for SearchLimit.  Defaults to DefaultSearchLimit.</param>
        internal void Uncancel(int? searchLimit = null)
        {
            SearchLimit = searchLimit??DefaultSearchLimit;
            isCanceling = false;
        }
        
        #region Fields
        /// <summary>
        /// Table of values assigned by this module to different global variables
        /// </summary>
        private readonly Dictionary<StateVariableName, object?> dictionary = new Dictionary<StateVariableName, object?>();
        /// <summary>
        /// Parent module to try if a variable can't be found in this module;
        /// </summary>
        public readonly Module? Parent;

        /// <summary>
        /// A user-defined procedure that can be called to import the value of a variable
        /// </summary>
        /// <param name="name">Variable to look up</param>
        /// <param name="value">Value found, if any</param>
        /// <returns>True if variable found</returns>
        public delegate bool BindHook(StateVariableName name, out object value);

        /// <summary>
        /// Optional list of hooks to try when a variable can't be found.
        /// </summary>
        private List<BindHook>? bindHooks;

        /// <summary>
        /// The global Module that all other modules inherit from by default.
        /// </summary>
        public static readonly Module Global;

        private List<WarningInfo>? loadTimeWarnings;

        /// <summary>
        /// Name of the module for debugging purposes
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Formatting options to use in Call.
        /// </summary>
        public FormattingOptions FormattingOptions = new FormattingOptions();

        /// <summary>
        /// Extension used for source files
        /// </summary>
        public string SourceExtension = ".step";

        /// <summary>
        /// Extension for CSV files
        /// </summary>
        public string CsvExtension = ".csv";
        #endregion

        #region Constructors
        static Module()
        {
            Global = new Module("Global", null);
            Builtins.DefineGlobals();
        }

        /// <summary>
        /// Make a module that inherits from Global
        /// </summary>
        public Module(string name) : this(name, Global)
        { }

        /// <summary>
        /// Make a module with the Global module as parent and load the specified source files into it.
        /// </summary>
        /// <param name="name">Name of the Module, for debugging purposes</param>
        /// <param name="parent">Parent module for this module.  This will usually be Module.Global</param>
        /// <param name="sourceFiles">Definition files to load</param>
        // ReSharper disable once UnusedMember.Global
        public Module(string name, Module parent, params string[] sourceFiles)
            : this(name, parent)
        {
            foreach (var f in sourceFiles)
                LoadDefinitions(f);
        }

        /// <summary>
        /// Make a module that inherits from the specified parent
        /// </summary>
        public Module(string name, Module? parent)
        {
            Parent = parent;
            Name = name;
        }
        #endregion

        #region Accessors
        /// <summary>
        /// Returns the value of the variable with the specified name
        /// </summary>
        /// <param name="variableName">Name (string) of the variable</param>
        /// <returns>Value</returns>
        /// <exception cref="UndefinedVariableException">If getting a variable and it is not listed in this module or its ancestors</exception>
        public object? this[string variableName]
        {
            get => this[StateVariableName.Named(variableName)];
            set => this[StateVariableName.Named(variableName)] = value;
        }

        /// <summary>
        /// Returns the value of the global variable with the specified name
        /// </summary>
        /// <param name="v">The variable</param>
        /// <returns>Value</returns>
        /// <exception cref="UndefinedVariableException">If getting a variable and it is not listed in this module or its ancestors</exception>
        public object? this[StateVariableName v]
        {
            get => Lookup(v);
            set => dictionary[v] = value;
        }

        /// <summary>
        /// True if this module has its own definition of the specified variable
        /// </summary>
        public bool Defines(string variableName) => Defines(StateVariableName.Named(variableName));

        /// <summary>
        /// True if this module has its own definition of the specified variable
        /// </summary>
        public bool Defines(StateVariableName v) => dictionary.ContainsKey(v);

        private object? Lookup(StateVariableName v, bool throwOnFailure = true)
        {
// First see if it's stored in this or some ancestor module
            for (var module = this; module != null; module = module.Parent)
                if (module.dictionary.TryGetValue(v, out var value))
                    return value;

            // Not found in this or any ancestor module; try bind hooks
            for (var module = this; module != null; module = module.Parent)
                if (module.bindHooks != null)
                    foreach (var hook in module.bindHooks)
                        if (hook(v, out var result))
                            return module.dictionary[v] = result;

            if (v.Name == "Mention")
                // if the user hasn't defined a Mention implementation, just use Write.
                return Builtins.WritePrimitive;
            
            // Give up
            if (throwOnFailure)
                throw new UndefinedVariableException(v);
            return null;
        }

        /// <summary>
        /// All the variable bindings in this module.
        /// </summary>
        public IEnumerable<KeyValuePair<StateVariableName, object?>> Bindings => dictionary;

        /// <summary>
        /// All the variable bindings in this module and its parent.
        /// </summary>
        public IEnumerable<KeyValuePair<StateVariableName, object?>> AllBindings =>
            Parent == null ? Bindings : Bindings.Concat(Parent.AllBindings);


        /// <summary>
        /// All CompoundTasks defined in this Module
        /// </summary>
        public IEnumerable<CompoundTask> DefinedTasks =>
            dictionary.Values.Where(x => x is CompoundTask).Cast<CompoundTask>();

        /// <summary>
        /// Find the CompoundTask named by the specified variable, creating one if necessary.
        /// </summary>
        /// <param name="v">Task variable</param>
        /// <param name="argCount">Number of arguments the task is expected to have</param>
        /// <param name="createIfNeeded">If true and variable is unbound, create a new task to bind it to.</param>
        /// <param name="path">Source file of method referencing this task, if relevant</param>
        /// <param name="lineNumber">Source file line number of method referencing this task, if relevant</param>
        /// <returns>The task</returns>
        /// <exception cref="ArgumentException">If variable is defined but isn't a CompoundTask</exception>
        internal CompoundTask? FindTask(StateVariableName v, int argCount, bool createIfNeeded = true, string? path = "Unknown", int lineNumber = 0)
        {
            CompoundTask? Recur(Module m)
            {
                if (m.dictionary.TryGetValue(v, out var value))
                {
                    var task = value as CompoundTask;
                    if (task == null)
                        throw new ArgumentException($"{v.Name} is not a compound task.  It is defined as {value}.");
                    return task;
                }
                if (m.Parent != null)
                    return Recur(m.Parent);
                return null;
            }

            var t = Recur(this);
            if (t == null)
            {
                if (createIfNeeded)
                {
                    // Define it in this module.
                    t = new CompoundTask(v.Name, argCount);
                    this[v] = t;
                }
                else 
                    return null;
            }
            if (t.ArgCount != argCount)
                throw new ArgumentException($"{Path.GetFileName(path)}:{lineNumber} {v.Name} was defined with {t.ArgCount} arguments, but a method is being added with {argCount} arguments");
            return t;
        }
        #endregion

        /// <summary>
        /// Run the call in the specified tuple.
        /// </summary>
        /// <param name="state">State in which to run the code</param>
        /// <param name="call">Tuple representing the task to call and its arguments</param>
        /// <exception cref="ArgumentException">If first element of call is not a task</exception>
        // ReSharper disable once UnusedMember.Global
        public (string? output, State newDynamicState) Eval(State state, object?[] call)
        {
            if (call.Length == 0)
                throw new ArgumentException("Attempt to evaluate a zero-length tuple");
            var task = call[0] as Task;
            if (task == null)
                throw new ArgumentException(
                    $"Attempt to evaluate something that isn't a task: {Writer.TermToString(call[0])}");
            return Call(state, task, call.Skip(1).ToArray());
        }

        /// <summary>
        /// Calls the named task with the specified arguments and returns the text it generates
        /// </summary>
        /// <param name="state">Global variable bindings to use in the call, if any.</param>
        /// <param name="taskName">Name of the task</param>
        /// <param name="args">Arguments to task, if any</param>
        /// <returns>Generated text as one big string, and final values of global variables.  Or null if the task failed.</returns>

        public (string? output, State newDynamicState) Call(
            State state, string taskName, params object?[] args)
        {
            var maybeTask = this[StateVariableName.Named(taskName)];
            var t = maybeTask as Task;
            if (t == null)
                throw new ArgumentException($"The value of the task argument to Call, {taskName}, is not a task.  It is {maybeTask}");
            return Call(state, t, args);
        }

        /// <summary>
        /// Calls the named task with the specified arguments and returns the text it generates
        /// </summary>
        /// <param name="state">Global variable bindings to use in the call, if any.</param>
        /// <param name="task">Task to call</param>
        /// <param name="args">Arguments to task, if any</param>
        /// <returns>Generated text as one big string, and final values of global variables.  Or null if the task failed.</returns>
        public (string? output, State newDynamicState) Call(
            State state, Task task, params object?[] args)
        {
            Uncancel();
var output = TextBuffer.NewEmpty();
            var env = new BindingEnvironment(this, null!, null, state);

            string? result = null;
            State newState = State.Empty;

            if (task.Call(args, output, env, null,
                (o, u, s, predecessor) =>
                {
                    result = o.Output.Untokenize(FormattingOptions);
                    newState = s;
                    MethodCallFrame.CurrentFrame = predecessor;
                    return true;
                }))
                return (result, newState);
            return (null, State.Empty);
        }

        /// <summary>
        /// Calls the named task with the specified arguments and returns the text it generates
        /// </summary>
        /// <param name="taskName">Name of the task</param>
        /// <param name="args">Arguments to task, if any</param>
        /// <returns>Generated text as one big string, and final values of global variables.  Or null if the task failed.</returns>
        public string? Call(string taskName, params object?[] args)
        {
            var (output, _) = Call(State.Empty, taskName, args);
            return output;
        }

        /// <summary>
        /// Calls the named task with the specified arguments as a predicate and returns true if it succeeds
        /// This call will fail if the task attempts to generate output.
        /// </summary>
        /// <param name="state">Global variable bindings to use in the call, if any.</param>
        /// <param name="taskName">Name of the task</param>
        /// <param name="args">Arguments to task, if any</param>
        public bool CallPredicate(
            State state, string taskName, params object?[] args)
        {
            Uncancel();
            var maybeTask = this[StateVariableName.Named(taskName)];
            var t = maybeTask as Task;
            if (t == null)
                throw new ArgumentException($"The value of the task argument to Call, {taskName}, is not a task.  It is {maybeTask}");
            var output = new TextBuffer(0);
            var env = new BindingEnvironment(this, null!, null, state);

            return t.Call(args, output, env, null, Interpreter.Step.SucceedContinuation);
        }

        /// <summary>
        /// Calls the named task with the specified arguments as a predicate and returns true if it succeeds
        /// This call will fail if the task attempts to generate output.
        /// </summary>
        /// <param name="taskName">Name of the task</param>
        /// <param name="args">Arguments to task, if any</param>
        public bool CallPredicate(string taskName, params object?[] args) => CallPredicate(State.Empty, taskName, args);

        private static readonly LocalVariableName FunctionResult = new LocalVariableName("??result", 0);

        /// <summary>
        /// Calls the named task with the specified arguments as a function and returns the value of its last argument
        /// This call will fail if the task attempts to generate output.
        /// </summary>
        /// <param name="state">Global variable bindings to use in the call, if any.</param>
        /// <param name="taskName">Name of the task</param>
        /// <param name="args">Arguments to task, if any</param>
        public T CallFunction<T>(
            State state, string taskName, params object?[] args)
        {
            Uncancel();
            var maybeTask = this[StateVariableName.Named(taskName)];
            var t = maybeTask as Task;
            if (t == null)
                throw new ArgumentException($"The value of the task argument to Call, {taskName}, is not a task.  It is {maybeTask}");

            var env = new BindingEnvironment(this, null!, null, state);
            var resultVar = new LogicVariable(FunctionResult);
            var extendedArgs = args.Append(resultVar).ToArray();

            var output = new TextBuffer(0);

            BindingList? bindings = null;

            if (!t.Call(extendedArgs, output, env, null,
                (o, u, s, p) =>
                {
                    bindings = u;
                    return true;
                })) 
                throw new CallFailedException(taskName, args, output);
            
            // Call succeeded; pull out the binding of the result variable and return it
            var finalEnv = new BindingEnvironment(this, null!, bindings, State.Empty);
            var result = finalEnv.CopyTerm(resultVar);
            if (result is LogicVariable)
                // resultVar is unbound or bound to an unbound variable
                throw new ArgumentInstantiationException(taskName, env, extendedArgs, output);
            return (T) result!;
        }

        /// <summary>
        /// Calls the named task with the specified arguments as a function and returns the value of its last argument
        /// This call will fail if the task attempts to generate output.
        /// </summary>
        /// <param name="taskName">Name of the task</param>
        /// <param name="args">Arguments to task, if any</param>
        public T CallFunction<T>(string taskName, params object?[] args) => CallFunction<T>(State.Empty, taskName, args);

        /// <summary>
        /// Call task on args and return the value of resultVar
        /// </summary>
        /// <exception cref="CallFailedException">If call to task fails</exception>
        /// <exception cref="ArgumentInstantiationException">If task fails to bind the result variable</exception>
        public T SolveFor<T>(Task task, object?[] args, LogicVariable resultVar, State? state = null)
        {
            if (state == null)
                state = State.Empty;
            Uncancel();
            var env = new BindingEnvironment(this, null!, null, state.Value);
            var output = new TextBuffer(0);

            BindingList? bindings = null;

            if (!task.Call(args, output, env, null,
                    (o, u, s, p) =>
                    {
                        bindings = u;
                        return true;
                    })) 
                throw new CallFailedException(task, args, output);
            
            // Call succeeded; pull out the binding of the result variable and return it
            var finalEnv = new BindingEnvironment(this, null!, bindings, State.Empty);
            var result = finalEnv.CopyTerm(resultVar);
            if (result is LogicVariable)
                // resultVar is unbound or bound to an unbound variable
                throw new ArgumentInstantiationException(task, env, args, output);
            return (T) result!;
        }

        /// <summary>
        /// Call task on args and return the value of resultVar
        /// </summary>
        /// <exception cref="CallFailedException">If call to task fails</exception>
        /// <exception cref="ArgumentInstantiationException">If task fails to bind the result variable</exception>
        public (T1,T2) SolveFor<T1,T2>(Task task, object?[] args, LogicVariable resultVar1, LogicVariable resultVar2, State? state = null)
        {
            if (state == null)
                state = State.Empty;
            Uncancel();
            var env = new BindingEnvironment(this, null!, null, state.Value);
            var output = new TextBuffer(0);

            BindingList? bindings = null;

            if (!task.Call(args, output, env, null,
                    (o, u, s, p) =>
                    {
                        bindings = u;
                        return true;
                    })) 
                throw new CallFailedException(task, args, output);
            
            // Call succeeded; pull out the binding of the result variable and return it
            var finalEnv = new BindingEnvironment(this, null!, bindings, State.Empty);
            var result1 = finalEnv.CopyTerm(resultVar1);
            if (result1 is LogicVariable)
                // resultVar is unbound or bound to an unbound variable
                throw new ArgumentInstantiationException(task, env, args, output);
            var result2 = finalEnv.CopyTerm(resultVar2);
            if (result2 is LogicVariable)
                // resultVar is unbound or bound to an unbound variable
                throw new ArgumentInstantiationException(task, env, args, output);
            return ((T1)result1!, (T2)result2!);
        }

        /// <summary>
        /// Call task on args and return the value of resultVar
        /// </summary>
        /// <exception cref="CallFailedException">If call to task fails</exception>
        /// <exception cref="ArgumentInstantiationException">If task fails to bind the result variable</exception>
        public (T1,T2,T3) SolveFor<T1,T2,T3>(Task task, object?[] args, LogicVariable resultVar1, LogicVariable resultVar2, LogicVariable resultVar3, State? state = null)
        {
            if (state == null)
                state = State.Empty;
            Uncancel();
            var env = new BindingEnvironment(this, null!, null, state.Value);
            var output = new TextBuffer(0);

            BindingList? bindings = null;

            if (!task.Call(args, output, env, null,
                    (o, u, s, p) =>
                    {
                        bindings = u;
                        return true;
                    })) 
                throw new CallFailedException(task, args, output);
            
            // Call succeeded; pull out the binding of the result variable and return it
            var finalEnv = new BindingEnvironment(this, null!, bindings, State.Empty);
            var result1 = finalEnv.CopyTerm(resultVar1);
            if (result1 is LogicVariable)
                // resultVar is unbound or bound to an unbound variable
                throw new ArgumentInstantiationException(task, env, args, output);
            var result2 = finalEnv.CopyTerm(resultVar2);
            if (result2 is LogicVariable)
                // resultVar is unbound or bound to an unbound variable
                throw new ArgumentInstantiationException(task, env, args, output);
            var result3 = finalEnv.CopyTerm(resultVar3);
            if (result3 is LogicVariable)
                // resultVar is unbound or bound to an unbound variable
                throw new ArgumentInstantiationException(task, env, args, output);
            return ((T1)result1!, (T2)result2!, (T3)result3!);
        }

        /// <summary>
        /// Load all source files in the specified directory
        /// </summary>
        /// <param name="path">Path for the directory</param>
        /// <param name="recursive">If true, load files from all directories in the subtree under path</param>
        public void LoadDirectory(string path, bool recursive = true)
        {
            MethodCallFrame.CurrentFrame = null;
            foreach (var file in Directory.GetFiles(path))
                // Load file if the filename ends with .step or .csv and doesn't start with .
                if (!Path.GetFileName(file).StartsWith(".") && (Path.GetExtension(file) == SourceExtension || Path.GetExtension(file) == CsvExtension))
                    LoadDefinitions(file);
            if (recursive)
                foreach (var sub in Directory.GetDirectories(path))
                    // ReSharper disable once RedundantArgumentDefaultValue
                    LoadDirectory(sub, true);
        }

        /// <summary>
        /// Load the definitions in the specified file
        /// </summary>
        /// <param name="path">Path to the file</param>
        public void LoadDefinitions(string path)
        {
            using var defs = new DefinitionStream(this, path);
            LoadDefinitions(defs);
        }

        /// <summary>
        /// Read definitions from a stream
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="path"></param>
        public void LoadDefinitions(TextReader reader, string? path) =>
            LoadDefinitions(new DefinitionStream(reader, this, path));

        /// <summary>
        /// Load the method definitions from stream into this module
        /// </summary>
        private void LoadDefinitions(DefinitionStream defs)
        {
            Uncancel();  // Just to be paranoid
            foreach (var (task, weight, pattern, locals, chain, flags, declaration, path, line) 
                in defs.Definitions)
            {
                if (task.Name == "initially")
                    RunLoadTimeInitialization(pattern, locals, chain, path, line);
                else
                {
                    var compoundTask = FindTask(task, pattern.Length, true, path, line)!;
                    if (locals == null)
                        // Declaration
                        compoundTask.Flags |= flags;
                    else
                        compoundTask.AddMethod(weight, pattern, locals, chain, flags, path, line);
                    compoundTask.Arglist ??= pattern.Select(x => x == null ? "?" : x.ToString()).ToArray();

                    if (declaration == "folder_structure")
                    {
                        var dir = Path.GetDirectoryName(defs.SourcePath);
                        Debug.Assert(dir != null, nameof(dir) + " != null");
                        DefineMethodsFromFolderStructure(compoundTask, dir);
                    }
                }
            }
        }

        private static readonly LocalVariableName[] NoLocals = Array.Empty<LocalVariableName>();
        private void DefineMethodsFromFolderStructure(CompoundTask task, string parentDirectory)
        {
            void Walk(string path, string? name)
            {
                foreach (var sub in Directory.GetDirectories(path))
                {
                    var subName = Path.GetFileName(sub);

                    if (subName.StartsWith("."))
                        continue;

                    var weight = 1f;
                    var weightPath = Path.Combine(sub, "weight.txt");
                    if (File.Exists(weightPath))
                    {
                        var contents = File.ReadAllLines(weightPath);
                        var weightString = contents.FirstOrDefault(s => !s.StartsWith("#"));
                        if (weightString == null || !float.TryParse(weightString, out weight))
                            throw new SyntaxError("Invalid format in weight.txt file", weightPath, 1);
                    }

                    task.Methods.Add(new Method(task, weight, new object?[] {name, subName}, NoLocals, null, null, 0));

                    Walk(sub, subName);
                }
            }

            Walk(parentDirectory, null);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void RunLoadTimeInitialization(object?[] pattern, LocalVariableName[]? locals, Interpreter.Step? chain, string? path, int line)
        {
            if (pattern.Length != 0)
                throw new SyntaxError("Initially command cannot take arguments", path, line);
            State bindings = State.Empty;
            var fakeInitiallyMethod = new Method(new CompoundTask("initially", 0), 1, Array.Empty<object?>(),
                Array.Empty<LocalVariableName>(), null, path, line);
            if (!Step.Interpreter.Step.Try(chain, new TextBuffer(0),
                new BindingEnvironment(this,
                    new MethodCallFrame(fakeInitiallyMethod, null, locals!.Select(name => new LogicVariable(name)).ToArray(), 
                        MethodCallFrame.CurrentFrame, MethodCallFrame.CurrentFrame)),
                (o, u, d, p ) =>
                {
                    bindings = d;
                    return true;
                },
                null))
                throw new InvalidOperationException($"{Path.GetFileName(path)}:{line} Initialization failed.");
            LoadBindingList(bindings);
        }

        private void LoadBindingList(State state)
        {
            foreach (var pair in state.Bindings)
                if (pair.Key is StateVariableName g)
                    this[g] = pair.Value;
        }

        /// <summary>
        /// Parse and add the method definitions to this module
        /// </summary>
        public void AddDefinitions(params string[] definitions)
        {
            foreach (var s in definitions)
                LoadDefinitions(new DefinitionStream(new StringReader(s), this, null));
        }

        /// <summary>
        /// Make a new module, then parse and add the specified method definitions.
        /// </summary>
        public static Module FromDefinitions(params string[] definitions)
        {
            var m = new Module("anonymous");
            m.AddDefinitions(definitions);
            return m;
        }

        /// <summary>
        /// Parse and run the specified code
        /// </summary>
        /// <param name="code">Code to run.  This will be used as the RHS of a method for the task TopLevelCall</param>
        /// <param name="state">State in which to execute the task.</param>
        /// <returns>Text output of the task and the resulting state</returns>
        public (string? output, State state) ParseAndExecute(string code, State state)
        {
            Uncancel();
            MethodCallFrame.CurrentFrame = null;
            if (Defines("TopLevelCall"))
                ((CompoundTask?) this["TopLevelCall"])?.EraseMethods();
            AddDefinitions($"TopLevelCall: {code}");
            return Call(state, "TopLevelCall");
        }

        /// <summary>
        /// Parse and run the specified code
        /// </summary>
        /// <param name="code">Code to run.  This will be used as the RHS of a method for the task TopLevelCall</param>
        /// <returns>Text output of the task</returns>
        public string? ParseAndExecute(string code) => ParseAndExecute(code, State.Empty).output;

        /// <summary>
        /// Add a procedure to call when a variable isn't found.
        /// If the procedure returns a value for it, that value is added to the module.
        /// </summary>
        /// <param name="hook">Procedure to use to import variables</param>
        // ReSharper disable once UnusedMember.Global
        public void AddBindHook(BindHook hook)
        {
            bindHooks ??= new List<BindHook>();
            bindHooks.Add(hook);
        }

        #region Debugging tools

        /// <summary>
        /// Returns any warnings found by code analysis
        /// </summary>
        public IEnumerable<WarningInfo> WarningsWithOffenders()
        {
            if (loadTimeWarnings != null)
                foreach (var w in loadTimeWarnings)
                    yield return w;
            foreach (var w in Lint())
                yield return w;
        }

        public IEnumerable<string> Warnings() => WarningsWithOffenders().Select(pair => pair.Warning);

        private IEnumerable<WarningInfo> Lint()
        {
            foreach (var pair in dictionary.ToArray())  // Copy the dictionary because it might get modified by TaskDefined
            {
                var variable = pair.Key;
                if (pair.Value is CompoundTask task)
                    foreach (var method in task.Methods)
                        for (var step = method.StepChain; step != null; step = step.Next)
                            if (step is Call { Task: StateVariableName g } && !TaskDefined(g))
                                yield return
                                    (method, $"{variable.Name} called undefined task {g.Name}");
            }

            foreach (var t in DefinedTasks)
                if ((t.Flags & CompoundTask.TaskFlags.Main) == 0 && !Documentation.IsUserDefinedSystemTask(t))
                {
                    var called = false;
                    foreach (var potentialCaller in DefinedTasks)
                        if (Callees(potentialCaller).Contains(t))
                        {
                            called = true;
                            break;
                        }

                    if (!called)
                        yield return (t, RichTextStackTraces?$"<b>{t}</b> is defined but never called.  If this is deliberate, you can add the annotation [main] to {t} to suppress this message." : $"{t} is defined but never called.    If this is deliberate, you can add the annotation [main] to {t} to suppress this message.");
                }
        }

        private bool TaskDefined(StateVariableName? stateVariableName)
        {
            if (stateVariableName == null)
                return false;

            var value = Lookup(stateVariableName, false);
            if (value == null)
                return false;

            switch (value)
            {
                case Task _:
                case Cons _:
                case bool _:
                case IList _:
                case IDictionary _:
                    return true;
                default:
                    return false;
            }
        }

        internal void AddWarning(string warning, object? offender=null)
        {
            loadTimeWarnings ??= new List<WarningInfo>();
            loadTimeWarnings.Add((offender, warning));
        }

        private object? ResolveVariables(object? o)
        {
            switch (o)
            {
                case StateVariableName v when dictionary.TryGetValue(v, out var result):
                    return result;
                case LocalVariableName l:
                    return new LogicVariable(l);
                case object[] tuple:
                    return tuple.Select(ResolveVariables).ToArray();
                default:
                    return o;
            }
        }

        private readonly Dictionary<CompoundTask, HashSet<object>> calleeTable = new Dictionary<CompoundTask, HashSet<object>>();
        internal HashSet<object> Callees(CompoundTask t)
        {
            if (calleeTable.TryGetValue(t, out var result))
                return result;
            
            var callees = new HashSet<object>();
            foreach (var c in t.Callees) callees.Add(ResolveVariables(c)!);

            calleeTable[t] = callees;
            return callees;
        }

        /// <summary>
        /// True if caller has a method that calls callee
        /// </summary>
        /// <param name="caller">Caller (must be a CompoundTask)</param>
        /// <param name="callee">Target of call (can be a CompoundTask or a primitive task, i.e. a delegate)</param>
        public bool TaskCalls(CompoundTask caller, object callee) => Callees(caller).Contains(callee);

        private readonly Dictionary<CompoundTask, object[][]> subtaskTable = new Dictionary<CompoundTask, object[][]>();

        internal object?[][] Subtasks(CompoundTask t)
        {
            if (subtaskTable.TryGetValue(t, out var result))
                return result;

            var subtasks = t.Calls.Select(c => c.Arglist.Select(ResolveVariables).Prepend(ResolveVariables(c.Task)).ToArray()).ToArray();

            subtaskTable[t] = subtasks!;
            return subtasks;
        }

        /// <summary>
        /// Return a trace of the method calls from the current frame.
        /// </summary>
        public static string StackTrace(BindingList? currentBindings = null)
        {
                var b = new StringBuilder();
                if (MethodCallFrame.CurrentFrame != null)
                {
                    currentBindings ??= MethodCallFrame.CurrentFrame.BindingsAtCallTime;
                    foreach (var frame in MethodCallFrame.CurrentFrame.CallerChain)
                        b.AppendLine(frame.GetCallSourceText(RichTextStackTraces, currentBindings));
                }
                return b.ToString();
        }
        #endregion

        /// <summary>
        /// An event handler to be called on every method call.
        /// Used to implement single-stepping in a debugger
        /// </summary>
        public delegate void TraceHandler(MethodTraceEvent traceEvent, Method method, object?[] args, TextBuffer output, BindingEnvironment env);

        /// <summary>
        /// An event handler to be called on every method call.
        /// Used to implement single-stepping in a debugger
        /// </summary>
        public TraceHandler? Trace;

        /// <summary>
        /// Which event is being traced (a call, success, or failure)
        /// </summary>
        public enum MethodTraceEvent
        {
            /// <summary>
            /// No recent trace event.
            /// </summary>
            None,
            /// <summary>
            /// The arguments have been matched to the head of this method and we will now try running its body
            /// </summary>
            Enter,
            /// <summary>
            /// The method succeeded
            /// </summary>
            Succeed,
            /// <summary>
            /// The method failed
            /// </summary>
            MethodFail,
            /// <summary>
            /// The call completely failed
            /// </summary>
            CallFail
        };
        
        internal void TraceMethod(MethodTraceEvent e, Method method, object?[] args, TextBuffer output, BindingEnvironment env)
        {
            if (--SearchLimit == 0)
            {
                if (isCanceling)
                    throw new TaskCanceledException();
                throw new StepTaskTimeoutException();
            }
            Trace?.Invoke(e, method, args, output, env);
        }

        /// <inheritdoc />
        public override string ToString() => $"<Module {Name}>";
    }
}
