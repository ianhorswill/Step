using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Step.Parser;

namespace Step.Output
{
    /// <summary>
    /// TokenFilters implement transformations done to output text before final untokenization
    /// They simply transform one token stream to another.
    /// </summary>
    public abstract class TokenFilter
    {
        #region Control tokens
        /// <summary>
        /// Control tokens are special string tokens used to indicate commands to token filters.
        /// Control tokens are strings, but they are compared for pointer equality; there is by definition
        /// only one copy of a given control token.  This means that unless you're using a version of the
        /// CLR that interns all strings, then even if you somehow manage to accidentally output a token that
        /// prints like a control token, it won't be mistaken for a control token.
        /// </summary>
        private static readonly HashSet<string> ControlTokens = new HashSet<string>(new ControlTokenEqualityComparer());

        /// <summary>
        /// Make a new control token.
        /// </summary>
        /// <param name="content">Some short, descriptive string indicating what the token is controlling</param>
        /// <returns>The created token</returns>
        public static string MakeControlToken(string content)
        {
            var token = $" [{content}] ";
            ControlTokens.Add(token);
            return token;
        }

        /// <summary>
        /// Make a new control token that is accessed with the substitution [content]
        /// </summary>
        /// <param name="content">Some short, descriptive string indicating what the token is controlling</param>
        /// <returns>The created token</returns>
        protected static string MakeControlTokenAndSubstitution(string content)
        {
            var token = MakeControlToken(content);
            DefinitionStream.DefineSubstitution(content, token);
            return token;
        }

        /// <summary>
        /// True if the token is one of the magic TokenFilter control tokens.
        /// </summary>
        /// <param name="token">a token output by a Step program</param>
        /// <returns>True if it was created by MakeControlToken()</returns>
        public static bool IsControlToken(string token) => ControlTokens.Contains(token);

        private class ControlTokenEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y) => ReferenceEquals(x, y);

            public int GetHashCode(string obj) => RuntimeHelpers.GetHashCode(obj);
        }
        #endregion

        /// <summary>
        /// Transform a sequence of tokens into a filtered (modified) sequence
        /// </summary>
        /// <param name="input">token sequence</param>
        /// <returns>Modified sequence</returns>
        public abstract IEnumerable<string> Filter(IEnumerable<string> input);

        /// <summary>
        /// Apply all the TokenFilters to the input, in order.
        /// So applying {a, b, c } to input returns c.Filter(b.Filter(a.Filter(input)))
        /// </summary>
        /// <param name="filters">Array of filters to apply</param>
        /// <param name="input">Token stream to filter</param>
        /// <returns>Filtered stream</returns>
        public static IEnumerable<string> ApplyFilters(TokenFilter[] filters, IEnumerable<string> input)
        {
            var result = input;
            foreach (var f in filters)
                result = f.Filter(result);
            return result;
        }

        /// <summary>
        /// Transforms a string (a, b, c) into a stream of pairs: ( (a,b), (b,c) , (c, null))
        /// </summary>
        protected static IEnumerable<(string, string?)> LookAhead(IEnumerable<string> input)
        {
            string? previous = null;
            foreach (var token in input)
            {
                if (previous != null)
                    yield return (previous, token);
                previous = token;
            }

            if (previous != null)
                yield return (previous, null);
        }

        /// <summary>
        /// Transforms a string (a, b, c, d) into a stream of triples: ( (a,b,c), (b,c,d) , (c, d, null), (d, null, null))
        /// </summary>
        public static IEnumerable<(string, string?, string?)> LookAhead2(IEnumerable<string> input)
        {
            string? first = null;
            string? second = null;
            foreach (var third in input)
            {
                if (first != null)
                    yield return (first, second, third);
                first = second;
                second = third;
            }

            if (first != null)
                yield return (first, second, null);

            if (second != null)
                yield return (second, null, null);
        }
    }
}
