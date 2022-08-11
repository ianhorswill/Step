using System.Collections.Generic;
using Step.Parser;

namespace Step.Output
{
    /// <summary>
    /// TokenFilters implement transformations done to output text before final untokenization
    /// They simply transform one token stream to another.
    /// </summary>
    public static class TokenFilter
    {
        internal static void DefineTokenMacros()
        {
            ExpressionStream.DefineSubstitution("a", AnOrAToken);
            ExpressionStream.DefineSubstitution("an", AnOrAToken);
        }

        /// <summary>
        /// A token that causes the system to write either "a" or "an" depending on the following token.
        /// </summary>
        public const string AnOrAToken = "[an]";

        public const string PresentTenseInfectionToken = "[s]";
        public const string ThirdPersonSingularToken = "[tps]";

        /// <summary>
        /// Transforms a string (a, b, c) into a stream of pairs: ( (a,b), (b,c) , (c, null))
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static IEnumerable<(string, string)> LookAhead(IEnumerable<string> input)
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

        /// <summary>
        /// Replaces [a] or [an] with "a" or "an" depending on whether the following token begins with a vowel.
        /// </summary>
        /// <param name="input">Token stream</param>
        /// <returns>Filtered token stream</returns>
        public static IEnumerable<string> AOrAnFilter(IEnumerable<string> input)
        {
            foreach (var (token, next) in LookAhead(input))
            {
                if (token == AnOrAToken)
                {
                    if (next == null || !TextUtilities.StartsWithVowel(next))
                        yield return "a";
                    else
                        yield return "an";
                }
                else
                    yield return token;
            }
        }

        //public static IEnumerable<string> ThirdPersonSingularFilter(IEnumerable<string> input)
        //{
        //    foreach (var (token, next) in LookAhead(input))
        //    {
        //        switch 
        //    }
        //}
    }
}
