using System.Collections.Generic;
using Step.Parser;

namespace Step.Output
{
    /// <summary>
    /// Replaces [a] or [an] with "a" or "an" depending on whether the following token begins with a vowel.
    /// </summary>
    public class AOrAnFilter : TokenFilter
    {
        /// <summary>
        /// Replaces [a] or [an] with "a" or "an" depending on whether the following token begins with a vowel.
        /// </summary>
        public static readonly AOrAnFilter Instance = new AOrAnFilter();

        /// <summary>
        /// A token that causes the system to write either "a" or "an" depending on the following token.
        /// </summary>
        private static readonly string AnOrAToken = MakeControlToken("an");

        static AOrAnFilter()
        {
            DefinitionStream.DefineSubstitution("a", AnOrAToken);
            DefinitionStream.DefineSubstitution("an", AnOrAToken);
        }

        /// <summary>
        /// Replaces [a] or [an] with "a" or "an" depending on whether the following token begins with a vowel.
        /// </summary>
        /// <param name="input">Token stream</param>
        /// <returns>Filtered token stream</returns>
        public override IEnumerable<string> Filter(IEnumerable<string> input)
        {
            foreach (var (token, next) in LookAhead(input))
            {
                if (ReferenceEquals(token, Output.AOrAnFilter.AnOrAToken))
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
    }
}
