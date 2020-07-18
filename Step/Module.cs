#region Copyright
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Step.Interpreter;
using Step.Parser;

namespace Step
{
    /// <summary>
    /// Stores values of global state variables
    /// </summary>
    public class Module
    {
        #region Fields
        /// <summary>
        /// Table of values assigned by this module to different global variables
        /// </summary>
        private readonly Dictionary<GlobalVariableName, object> dictionary = new Dictionary<GlobalVariableName, object>();
        /// <summary>
        /// Parent module to try if a variable can't be found in this module;
        /// </summary>
        public readonly Module Parent;

        /// <summary>
        /// A user-defined procedure that can be called to import the value of a variable
        /// </summary>
        /// <param name="name">Variable to look up</param>
        /// <param name="value">Value found, if any</param>
        /// <returns>True if variable found</returns>
        public delegate bool BindHook(GlobalVariableName name, out object value);

        /// <summary>
        /// Optional list of hooks to try when a variable can't be found.
        /// </summary>
        private List<BindHook> bindHooks;

        /// <summary>
        /// The global Module that all other modules inherit from by default.
        /// </summary>
        public static readonly Module Global;

        private List<string> loadTimeWarnings;
        #endregion

        #region Constructors
        static Module()
        {
            Global = new Module(null);
            Builtins.DefineGlobals();
        }

        /// <summary>
        /// Make a module that inherits from Global
        /// </summary>
        public Module() : this(Global)
        { }

        /// <summary>
        /// Make a module with the Global module as parent and load the specified source files into it.
        /// </summary>
        /// <param name="parent">Parent module for this module.  This will usually be Module.Global</param>
        /// <param name="sourceFiles">Definition files to load</param>
        public Module(Module parent, params string[] sourceFiles)
            : this(parent)
        {
            foreach (var f in sourceFiles)
                LoadDefinitions(f);
        }

        /// <summary>
        /// Make a module that inherits from the specified parent
        /// </summary>
        public Module(Module parent)
        {
            Parent = parent;
        }
        #endregion

        #region Accessors
        /// <summary>
        /// Returns the value of the variable with the specified name
        /// </summary>
        /// <param name="variableName">Name (string) of the variable</param>
        /// <returns>Value</returns>
        /// <exception cref="UndefinedVariableException">If getting a variable and it is not listed in this module or its ancestors</exception>
        public object this[string variableName]
        {
            get => this[GlobalVariableName.Named(variableName)];
            set => this[GlobalVariableName.Named(variableName)] = value;
        }

        /// <summary>
        /// Returns the value of the global variable with the specified name
        /// </summary>
        /// <param name="v">The variable</param>
        /// <returns>Value</returns>
        /// <exception cref="UndefinedVariableException">If getting a variable and it is not listed in this module or its ancestors</exception>
        public object this[GlobalVariableName v]
        {
            get => Lookup(v);
            set => dictionary[v] = value;
        }

        private object Lookup(GlobalVariableName v, bool throwOnFailure = true)
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

            // Give up
            if (throwOnFailure)
                throw new UndefinedVariableException(v);
            return null;
        }

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
        internal CompoundTask FindTask(GlobalVariableName v, int argCount, bool createIfNeeded = true, string path = "Unknown", int lineNumber = 0)
        {
            CompoundTask Recur(Module m)
            {
                if (m.dictionary.TryGetValue(v, out var value))
                {
                    var task = value as CompoundTask;
                    if (task == null)
                        throw new ArgumentException($"{v.Name} is not a task.  It is defined as {value}.");
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
        /// Calls the named task with the specified arguments and returns the text it generates
        /// </summary>
        /// <param name="dynamicState">Global variable bindings to use in the call, if any.</param>
        /// <param name="taskName">Name of the task</param>
        /// <param name="args">Arguments to task, if any</param>
        /// <returns>Generated text as one big string, and final values of global variables.  Or null if the task failed.</returns>
        public (string output, DynamicState newDynamicState) Call(
            DynamicState dynamicState, string taskName, params object[] args)
        {
            var maybeTask = this[GlobalVariableName.Named(taskName)];
            var t = maybeTask as CompoundTask;
            if (t == null)
                throw new ArgumentException($"{taskName} is a task.  Its value is {maybeTask}");
            var output = PartialOutput.NewEmpty();
            var env = new BindingEnvironment(this, null, null, dynamicState.Bindings);

            string result = null;
            BindingList<GlobalVariableName> newState = null;

            foreach (var method in t.EffectiveMethods)
                if (method.Try(args, output, env, (o, u, s) => { result = o.AsString; newState = s; return true; }))
                    return (result, new DynamicState(newState));
            return (null, new DynamicState(null));
        }

        /// <summary>
        /// Calls the named task with the specified arguments and returns the text it generates
        /// </summary>
        /// <param name="taskName">Name of the task</param>
        /// <param name="args">Arguments to task, if any</param>
        /// <returns>Generated text as one big string, and final values of global variables.  Or null if the task failed.</returns>
        public string Call(string taskName, params object[] args)
        {
            var (output, _) = Call(new DynamicState(null), taskName, args);
            return output;
        }

        /// <summary>
        /// Load the definitions in the specified file
        /// </summary>
        /// <param name="path">Path to the file</param>
        public void LoadDefinitions(string path)
        {
            using (var f = File.OpenText(path))
                LoadDefinitions(f, path);
        }

        /// <summary>
        /// Load the method definitions from stream into this module
        /// </summary>
        public void LoadDefinitions(TextReader stream, string filePath)
        {
            foreach (var (task, pattern, locals, chain, flags, path, line) in new DefinitionStream(stream, this, filePath).Definitions)
                FindTask(task, pattern.Length, true, path, line).AddMethod(pattern, locals, chain, flags, path, line);
        }

        /// <summary>
        /// Parse and add the method definitions to this module
        /// </summary>
        public void AddDefinitions(params string[] definitions)
        {
            foreach (var s in definitions)
                LoadDefinitions(new StringReader(s), null);
        }

        /// <summary>
        /// Make a new module, then parse and add the specified method definitions.
        /// </summary>
        public static Module FromDefinitions(params string[] definitions)
        {
            var m = new Module();
            m.AddDefinitions(definitions);
            return m;
        }

        /// <summary>
        /// Add a procedure to call when a variable isn't found.
        /// If the procedure returns a value for it, that value is added to the module.
        /// </summary>
        /// <param name="hook">Procedure to use to import variables</param>
        public void AddBindHook(BindHook hook)
        {
            if (bindHooks == null)
                bindHooks = new List<BindHook>();
            bindHooks.Add(hook);
        }

        #region Debugging tools

        /// <summary>
        /// Returns any warnings found by code analysis
        /// </summary>
        public IEnumerable<string> Warnings()
        {
            if (loadTimeWarnings != null)
                foreach (var w in loadTimeWarnings)
                    yield return w;
            foreach (var w in Lint())
                yield return w;
        }

        private IEnumerable<string> Lint()
        {
            foreach (var pair in dictionary.ToArray())  // Copy the dictionary because it might get modified by TaskDefined
            {
                var variable = pair.Key;
                if (pair.Value != null && pair.Value is CompoundTask task)
                    foreach (var method in task.Methods)
                        for (var step = method.StepChain; step != null; step = step.Next)
                            if (step is Call c && c.Task is GlobalVariableName g && !TaskDefined(g))
                            {
                                var fileName = method.FilePath == null ? "Unknown" : Path.GetFileName(method.FilePath);
                                yield return
                                    $"{fileName}:{method.LineNumber} {variable.Name} called undefined task {g.Name}";
                            }
            }
        }

        private bool TaskDefined(GlobalVariableName globalVariableName)
        {
            if (globalVariableName == null)
                return false;

            var value = Lookup(globalVariableName, false);
            if (value == null)
                return false;

            value = PrimitiveTask.GetSurrogate(value);

            switch (value)
            {
                case null:
                    return false;
                case CompoundTask _:
                case Delegate _:
                    return true;
                default:
                    return false;
            }
        }

        internal void AddWarning(string warning)
        {
            if (loadTimeWarnings == null)
                loadTimeWarnings = new List<string>();
            loadTimeWarnings.Add(warning);
        }

        /// <summary>
        /// Return a trace of the method calls from the current frame.
        /// </summary>
        public static string StackTrace
        {
            get
            {
                var b = new StringBuilder();
                for (var frame = MethodCallFrame.CurrentFrame; frame != null; frame = frame.Parent) 
                    b.AppendLine(frame.CallSourceText);
                return b.ToString();
            }
        }
        #endregion
    }
}
