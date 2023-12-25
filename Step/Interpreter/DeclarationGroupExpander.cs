using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Step.Output;
using Step.Parser;

namespace Step.Interpreter
{
    internal class DeclarationGroupExpander
    {
        public const string GroupPredicate = "DeclarationGroup";
        public const string ExpansionFunction = "DeclarationExpansion";

        public readonly Module Module;

        /// <summary>
        /// Declaration group currently in effect or null
        /// </summary>
        public object?[]? CurrentDeclarationGroup { get; private set; }

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
        /// <returns>Expanded head, or original if no expansion</returns>
        public (string taskName, object?[] pattern) ExpandHead((string taskName, IList<Object?> pattern) head, string? path, int lineNumber)
        {
            if (CurrentDeclarationGroup == null) return (head.taskName, head.pattern.ToArray());
            if (!Module.Defines(ExpansionFunction))
                throw new SyntaxError(
                    $"Declaration group {Writer.TermToString(head)} invoked without {ExpansionFunction} being defined",
                    path, lineNumber);
            try
            {
                var headTuple = head.pattern.Prepend(head.taskName).ToArray();
                var newHeadTuple = Module.CallFunction<object?[]>(ExpansionFunction, CurrentDeclarationGroup, headTuple);
                if (newHeadTuple.Length < 1 && !(newHeadTuple[0] is string))
                    throw new SyntaxError(
                        $"Expansion of {Writer.TermToString(headTuple)} within declaration group {Writer.TermToString(CurrentDeclarationGroup)} returned invalid expansion {Writer.TermToString(newHeadTuple)}",
                        path, lineNumber);
                return ((string)newHeadTuple[0]!, newHeadTuple.Skip(1).ToArray());
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
        /// <returns>True if it was a group attribute invocation</returns>
        public bool HandleGroupAttribute(object?[] possibleGroupAttribute)
        {
            if (!IsDeclarationGroup(possibleGroupAttribute))
                return false;
            CurrentDeclarationGroup = possibleGroupAttribute;
            return true;
        }

        /// <summary>
        /// True if [name args] is a valid invocation of a declaration group
        /// </summary>
        /// <param name="attribute">The attribute expression that appeared in the source file</param>
        /// <param name="m">Module into which the code is being loaded</param>
        public bool IsDeclarationGroup(object?[] attribute)
            => Module.Defines(GroupPredicate) && Module.CallPredicate(GroupPredicate, new object?[] { attribute });
    }
}
