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
        private readonly Dictionary<GlobalVariableName, object> dictionary = new Dictionary<GlobalVariableName, object>();
        public readonly Module Parent;
        public static readonly Module Global;
        #endregion

        #region Constructors
        static Module()
        {
            Global = new Module(null);
            PrimitiveTask.DefineGlobals();
        }

        /// <summary>
        /// Make a module that inherits from Global
        /// </summary>
        public Module() : this(Global)
        { }

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
            get
            {
                if (dictionary.TryGetValue(v, out var value))
                    return value;
                if (Parent != null)
                    return Parent[v];
                throw new UndefinedVariableException(v);
            }
            set => dictionary[v] = value;
        }

        /// <summary>
        /// Find the CompoundTask named by the specified variable, creating one if necessary.
        /// </summary>
        /// <param name="v">Task variable</param>
        /// <param name="argCount">Number of arguments the task is expected to have</param>
        /// <returns>The task</returns>
        /// <exception cref="ArgumentException">If variable is defined but isn't a CompoundTask</exception>
        private CompoundTask FindTask(GlobalVariableName v, int argCount)
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
                // Define it in this module.
                t = new CompoundTask(v.Name, argCount);
                this[v] = t;
            }
            if (t.ArgCount != argCount)
                throw new ArgumentException($"{v.Name} was defined with {t.ArgCount} arguments, but a method is being added with {argCount} arguments");
            return t;
        }
        #endregion

        /// <summary>
        /// Calls the named task with the specified arguments and returns the text it generates
        /// </summary>
        /// <param name="taskName">Name of the task</param>
        /// <param name="args">Arguments to task, if any</param>
        /// <returns>Generated text as one big string, or null if the task failed.</returns>
        public string Call(string taskName, params object[] args)
        {
            var maybeTask = this[GlobalVariableName.Named(taskName)];
            var t = maybeTask as CompoundTask;
            if (t == null)
                throw new ArgumentException($"{taskName} is a task.  Its value is {maybeTask}");
            return t.Call(this, args);
        }

        /// <summary>
        /// Load the method definitions from stream into this module
        /// </summary>
        public void LoadDefinitions(TextReader stream)
        {
            foreach (var (task, pattern, locals, chain) in new DefinitionStream(stream).Definitions)
                FindTask(task, pattern.Length).AddMethod(pattern, locals, chain);
        }

        /// <summary>
        /// Parse and add the method definitions to this module
        /// </summary>
        public void AddDefinitions(params string[] definitions)
        {
            foreach (var s in definitions)
                LoadDefinitions(new StringReader(s));
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
    }
}
