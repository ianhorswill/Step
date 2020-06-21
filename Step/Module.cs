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
using System.Xml.Schema;
using Step.Interpreter;
using Step.Parser;

namespace Step
{
    /// <summary>
    /// Stores values of global state variables
    /// </summary>
    public class Module
    {
        private readonly Dictionary<GlobalVariable, object> dictionary = new Dictionary<GlobalVariable, object>();
        public readonly Module Parent;
        public static readonly Module Global;

        static Module()
        {
            Global = new Module(null);
            PrimitiveTask.DefineGlobals();
        }

        public Module() : this(Global)
        { }

        public Module(Module parent)
        {
            Parent = parent;
        }

        public object this[string variableName]
        {
            get => this[GlobalVariable.Named(variableName)];
            set => this[GlobalVariable.Named(variableName)] = value;
        }

        public object this[GlobalVariable v]
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

        private CompoundTask FindTask(GlobalVariable v, int argCount)
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

        public string Call(string taskName, params object[] args)
        {
            var maybeTask = this[GlobalVariable.Named(taskName)];
            var t = maybeTask as CompoundTask;
            if (t == null)
                throw new ArgumentException($"{taskName} is a task.  Its value is {maybeTask}");
            return new Call(t, args, null).Expand(this);
        }

        public void LoadDefinitions(TextReader stream)
        {
            foreach (var (task, pattern, locals, chain) in new DefinitionStream(stream).Definitions)
                FindTask(task, pattern.Length).AddMethod(pattern, locals, chain);
        }

        public void AddDefinitions(params string[] definitions)
        {
            foreach (var s in definitions)
                LoadDefinitions(new StringReader(s));
        }

        public static Module FromDefintions(params string[] definitions)
        {
            var m = new Module();
            m.AddDefinitions(definitions);
            return m;
        }
    }
}
