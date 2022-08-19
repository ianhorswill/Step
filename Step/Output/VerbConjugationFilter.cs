using System.Collections.Generic;

namespace Step.Output
{
    /// <summary>
    /// Tracks linguistic features of sentence subject and inflects verbs when asked to
    /// </summary>
    public class VerbConjugationFilter : TokenFilter
    {
        /// <summary>
        /// Tracks linguistic features of sentence subject and inflects verbs when asked to
        /// </summary>
        public static VerbConjugationFilter Instance = new VerbConjugationFilter();

        private static readonly string InflectVerbToken = MakeControlTokenAndSubstitution("s");
        private static readonly string InflectVerbEsToken = MakeControlTokenAndSubstitution("es");

        private static readonly string IsToken = MakeControlTokenAndSubstitution("is");
        private static readonly string HasToken = MakeControlTokenAndSubstitution("has");

        private static readonly string SingularToken = MakeControlTokenAndSubstitution("singular");
        private static readonly string PluralToken = MakeControlTokenAndSubstitution("plural");

        private static readonly string FirstPersonToken = MakeControlTokenAndSubstitution("firstPerson");
        private static readonly string SecondPersonToken = MakeControlTokenAndSubstitution("secondPerson");
        private static readonly string ThirdPersonToken = MakeControlTokenAndSubstitution("thirdPerson");

        private static readonly string[][] IsConjugations =
        {
            new[] {"am", "are"},
            new[] {"are", "are"},
            new[] {"is", "are"}
        };

        private static readonly string[][] HasConjugations =
        {
            new[] {"have", "have"},
            new[] {"have", "have"},
            new[] {"has", "have"}
        };


        /// <summary>
        /// Tracks linguistic features of sentence subject and inflects verbs when asked to
        /// </summary>
        public override IEnumerable<string> Filter(IEnumerable<string> input)
        {
            var number = Inflection.Number.Singular;
            var person = Inflection.Person.Third;

            foreach (var (token, next) in LookAhead(input))
            {
                if (ReferenceEquals(token, SingularToken))
                    number = Inflection.Number.Singular;
                else if (ReferenceEquals(token, PluralToken))
                    number = Inflection.Number.Plural;
                else if (ReferenceEquals(token, FirstPersonToken))
                    person = Inflection.Person.First;
                else if (ReferenceEquals(token, SecondPersonToken))
                    person = Inflection.Person.Second;
                else if (ReferenceEquals(token, ThirdPersonToken))
                    person = Inflection.Person.Third;
                else if (ReferenceEquals(token, IsToken))
                    yield return IsConjugations[(int)person][(int)number];
                else if (ReferenceEquals(token, HasToken))
                    yield return HasConjugations[(int)person][(int)number];
                else
                {
                    if (!ReferenceEquals(token, InflectVerbToken))
                    {
                        if ((ReferenceEquals(next, InflectVerbToken) || ReferenceEquals(next, InflectVerbEsToken))
                            && person == Inflection.Person.Third && number == Inflection.Number.Singular)
                        {
                            yield return Inflection.ThirdPersonSingularFormOfEnglishVerb(token, ReferenceEquals(next, InflectVerbEsToken) ? "es" : "s");
                        }
                        else 
                            yield return token;
                    }
                }
            }
        }
    }
}
