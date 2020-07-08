using System.Linq;
using static Step.Interpreter.PrimitiveTask;

namespace Step.Interpreter
{
    /// <summary>
    /// Implementations of built-in, but first-order primitives
    /// Higher-order primitives are in HigherOrderBuiltins.cs
    /// </summary>
    internal static class Builtins
    {
        private static readonly string[] NewLine = { "\n" };
        /// <summary>
        /// Add the built-in primitives to the global module.
        /// </summary>
        internal static void DefineGlobals()
        {
            var g = Module.Global;

            g["="] = Predicate<int, int>("=", (a, b) => a == b);
            g[">"] = Predicate<int, int>(">", (a, b) => a > b);
            g["<"] = Predicate<int, int>("<", (a, b) => a < b);
            g[">="] = Predicate<int, int>(">=", (a, b) => a >= b);
            g["<="] = Predicate<int, int>("<=", (a, b) => a <= b);
            g["Newline"] = (PrimitiveTask.DeterministicTextGenerator0) (() => NewLine);

            HigherOrderBuiltins.DefineGlobals();
        }
    }
}
