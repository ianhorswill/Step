using System;
using System.Collections.Generic;
using System.Linq;
using Step.Interpreter;
using Step.Output;

namespace Step.Parser
{
    internal class DeclarationGroupExpander
    {
        public const string GroupPredicate = "DeclarationGroup";
        public const string ExpansionFunction = "DeclarationExpansion";

        public readonly Module Module;

        internal Queue<(StateVariableName task, object?[] pattern)> Assertions = new Queue<(StateVariableName, object?[])>();

        /// <summary>
        /// Declaration group currently in effect or null
        /// </summary>
        public object?[]? CurrentDeclarationGroup { get; private set; }

        private readonly HashSet<string> currentDeclarationGroupVariableNames = new HashSet<string>();

        public bool IsVariableFromCurrentDeclarationGroup(LocalVariableName l) => currentDeclarationGroupVariableNames.Contains(l.Name);

        public DeclarationGroupExpander(Module module)
        {
            Module = module;
        }

        /// <summary>
        /// Return the expanded version of this declaration head.
        /// If there is no declaration group in effect, return the original head.
        /// If there is a declaration group in effect, but this head cannot be expanded by it, then cancel the current group and return the original head.
        /// </summary>
        /// <param name="head">Head to expand</param>
        /// <param name="getLocal">Generator for local variables for the enclosing method</param>
        /// <param name="path">Source file from which this comes, if any</param>
        /// <param name="lineNumber">Line number within source file, if any</param>
        /// <returns>Expanded head, or original if no expansion</returns>
        public (string taskName, object?[] pattern) ExpandHead((string taskName, IList<Object?> pattern) head, Func<string, LocalVariableName> getLocal, string? path, int lineNumber)
        {
            if (CurrentDeclarationGroup == null) return (head.taskName, head.pattern.ToArray());

            object? Variablize(object? o) => o switch
            {
                null => null,
                string s when DefinitionStream.IsLocalVariableName(s) => getLocal(s),
                object?[] tuple => tuple.Select(Variablize).ToArray(),
                _ => o
            };

            object? Devariablize(object? o) => o switch
            {
                null => null,
                LocalVariableName v => v.Name,
                object?[] tuple => tuple.Select(Devariablize).ToArray(),
                _ => o
            };

            try
            {
                var headTuple = head.pattern.Select(Devariablize).Prepend(head.taskName).ToArray();
                var newHeadTuple = Module.CallFunction<object?[]>(ExpansionFunction, CurrentDeclarationGroup, headTuple);
                if (newHeadTuple.Length < 1 && !(newHeadTuple[0] is string))
                    throw new SyntaxError(
                        $"Expansion of {Writer.TermToString(headTuple)} within declaration group {Writer.TermToString(CurrentDeclarationGroup)} returned invalid expansion {Writer.TermToString(newHeadTuple)}",
                        path, lineNumber);
                return ((string)newHeadTuple[0]!, newHeadTuple.Skip(1).Select(Variablize).ToArray());
            }
            catch (CallFailedException)
            {
                CurrentDeclarationGroup = null;
                return (head.taskName, head.pattern.ToArray());
            }
        }

        /// <summary>
        /// Set current declaration group to the argument, if it is indeed a declaration group attribute
        /// </summary>
        /// <param name="possibleGroupAttribute">Attribute appearing in the source code</param>
        /// <param name="path">Path of source file, if any</param>
        /// <param name="lineNumber">Line number within source file</param>
        /// <returns>True if it was a group attribute invocation</returns>
        public bool HandleGroupAttribute(object?[] possibleGroupAttribute, string? path, int lineNumber)
        {
            var head = possibleGroupAttribute[0];
            if (!(head is string))
                return false;
            if (!IsDeclarationGroup(possibleGroupAttribute))
                return false;

            if (!Module.Defines(ExpansionFunction))
                throw new SyntaxError(
                    $"Declaration group {Writer.TermToString(head)} invoked without {ExpansionFunction} being defined",
                    path, lineNumber);

            MaybeAddHeaderAssertion(possibleGroupAttribute, path, lineNumber);
            
            CurrentDeclarationGroup = possibleGroupAttribute;

            currentDeclarationGroupVariableNames.Clear();

            void FindVariables(object?[] tuple)
            {
                foreach (var element in tuple)
                    switch (element)
                    {
                        case string s when DefinitionStream.IsLocalVariableName(s):
                            currentDeclarationGroupVariableNames.Add(s);
                            break;
                        case object?[] subtuple:
                            FindVariables(subtuple);
                            break;
                    }
            }

            FindVariables(CurrentDeclarationGroup);

            return true;
        }

        private void MaybeAddHeaderAssertion(object?[] headTuple, string? path, int lineNumber)
        {
            try
            {
                var assertion = Module.CallFunction<object?[]>(ExpansionFunction, headTuple, headTuple);
                if (assertion.Length < 1 && !(assertion[0] is string))
                    throw new SyntaxError(
                        $"Expansion of {Writer.TermToString(headTuple)} within declaration group {Writer.TermToString(CurrentDeclarationGroup)} returned invalid expansion {Writer.TermToString(assertion)}",
                        path, lineNumber);

                Assertions.Enqueue((StateVariableName.Named((string)assertion[0]!), assertion.Skip(1).ToArray()));
            }
            catch (CallFailedException)
            {
                // Do nothing
            }
        }

        /// <summary>
        /// True if [name args] is a valid invocation of a declaration group
        /// </summary>
        /// <param name="attribute">The attribute expression that appeared in the source file</param>
        public bool IsDeclarationGroup(object?[] attribute)
            => Module.Defines(GroupPredicate) && Module.CallPredicate(GroupPredicate, new object?[] { attribute });
    }
}
