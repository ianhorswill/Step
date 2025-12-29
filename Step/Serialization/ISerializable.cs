namespace Step.Serialization
{
    public interface ISerializable
    {
        public void Serialize(Serializer s);

        public string SerializationTypeToken() => GetType().Name;

        public (char start, string typeToken, char end, bool includeSpace) SerializationBracketing() => ('<', SerializationTypeToken(), '>', true);
    }
}
