using System.Collections.Generic;
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
        private static readonly HashSet<object> ControlTokens = new HashSet<object>();

        /// <summary>
        /// Make a new control token.
        /// </summary>
        /// <param name="content">Some short, descriptive string indicating what the token is controlling</param>
        /// <returns>The created token</returns>
        protected static string MakeControlToken(string content)
        {
            var token = $" [{content}] ";
            ControlTokens.Add(token);
            return token;
        }

        /// <summary>
        /// True if the token is one of the magic TokenFilter control tokens.
        /// </summary>
        /// <param name="token">a token output by a Step program</param>
        /// <returns>True if it was created by MakeControlToken()</returns>
        public static bool IsControlToken(string token) => ControlTokens.Contains(token);
        #endregion



        

        public static readonly string PresentTenseInfectionToken = MakeControlToken("s");
        public static readonly string ThirdPersonSingularToken = MakeControlToken("tps");

        public abstract IEnumerable<string> Filter(IEnumerable<string> input);

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
        /// <param name="input"></param>
        /// <returns></returns>
        protected static IEnumerable<(string, string)> LookAhead(IEnumerable<string> input)
        {
            string previous = null;
            foreach (var token in input)
            {
                if (previous != null)
                    yield return (previous, token);
                previous = token;
            }

            if (previous != null)
                yield return (previous, null);
        }

        //public static IEnumerable<(string, string, string)> LookAhead2(IEnumerable<string> input)
        //{
        //    string first = null;
        //    string second = null;
        //    foreach (var third in input)
        //    {
        //        if (first != null)
        //            yield return (first, second, third);
        //        first = second;
        //        second = third;
        //    }

        //    if (first != null)
        //        yield return (first, second, null);
        //}



        //public static IEnumerable<string> ThirdPersonSingularFilter(IEnumerable<string> input)
        //{
        //    foreach (var (token, next) in LookAhead(input))
        //    {
        //        switch 
        //    }
        //}
    }
}
