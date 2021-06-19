namespace Step.Parser
{
    class TupleExpression
    {
        public readonly object[] Elements;
        public string BracketStyle;

        public TupleExpression(string bracketStyle, object[] elements)
        {
            BracketStyle = bracketStyle;
            Elements = elements;
        }
    }
}
