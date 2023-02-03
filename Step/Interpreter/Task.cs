using System.Collections.Generic;
using System.Diagnostics;
using Step.Serialization;

namespace Step.Interpreter
{
    /// <summary>
    /// Base class for objects representing tasks (things user code can call)
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public abstract class Task : ISerializable
    {
        static Task()
        {
            Deserializer.RegisterHandler(nameof(Task), Deserialize);
        }

        /// <summary>
        /// Name, for debugging purposes
        /// </summary>
        public readonly string Name;

        private Dictionary<string, object>? propertyDictionary;

        /// <summary>
        /// Dictionary of additional user-defined metadata
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public IDictionary<string, object> Properties => propertyDictionary ?? (propertyDictionary = new Dictionary<string, object>());

        /// <summary>
        /// Number of arguments required by this task, if fixed.
        /// </summary>
        public readonly int? ArgumentCount;

        /// <summary>
        /// Optional documentation of the names of the arguments to the task
        /// These are here purely for documentation purposes.  They aren't used during the calling sequence.
        /// </summary>
        public string[]? Arglist;

        /// <summary>
        /// This task has documentation metadata
        /// </summary>
        public bool HasDocumentation => Arglist != null && Description != null;

        /// <summary>
        /// Adds documentation of the arguments to the task
        /// </summary>
        /// <param name="arglist">The names of the arguments</param>
        /// <returns>The original task</returns>
        public Task Arguments(params string[] arglist)
        {
            Arglist = arglist;
            return this;
        }

        /// <summary>
        /// Optional documentation of what the task does
        /// </summary>
        public string? Description;

        /// <summary>
        /// Adds documentation of the description of the task
        /// </summary>
        /// <param name="documentation">Description of what the task does</param>
        /// <returns>The original task</returns>
        public Task Documentation(string documentation)
        {
            Description = documentation;
            return this;
        }

        /// <summary>
        /// Optional documentation of what section of the manual this should appear in
        /// </summary>
        public string? ManualSection;

        /// <summary>
        /// Adds documentation of the description of the task
        /// </summary>
        /// <param name="manualSection">Section of the manual to place this task in</param>
        /// <param name="documentation">Description of what the task does</param>
        /// <returns>The original task</returns>
        public Task Documentation(string manualSection, string documentation)
        {
            ManualSection = manualSection;
            Description = documentation;
            return this;
        }

        /// <summary>
        /// Initialize name of task
        /// </summary>
        protected Task(string name, int? argumentCount)
        {
            Name = name;
            ArgumentCount = argumentCount;
        }

        /// <summary>
        /// Call this task with the specified arguments
        /// </summary>
        /// <param name="arglist">Task arguments</param>
        /// <param name="output">Output accumulated so far</param>
        /// <param name="env">Binding environment</param>
        /// <param name="predecessor">Most recently succeeded MethodCallFrame</param>
        /// <param name="k">Continuation</param>
        /// <returns>True if task succeeded and continuation succeeded</returns>
        /// <exception cref="CallFailedException">If the task fails</exception>
        public abstract bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k);

        /// <inheritdoc />
        public override string ToString() => Name;

        public void Serialize(Serializer s)
        {
            s.Write(Name);
        }

        private static object? Deserialize(Deserializer d)
        {
            var taskName = d.ReadAlphaNumeric();
            return d.Module[taskName];
        }

        public string SerializationTypeToken() => "Task";
    }
}
