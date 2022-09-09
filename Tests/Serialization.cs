using Step;
using Step.Serialization;

namespace Tests
{
    [TestClass]
    public class Serialization
    {
        private bool TestEqual(object a, object b)
        {
            if (a is State sa && b is State sb)
                return State.TestEqual(sa, sb);
            return Equals(a, b);
        }

        private void TestRoundTrip(object o)
        {
            var serialized = Serializer.SerializeToString(o);
            var r = Deserializer.Deserialize(new StringReader(serialized));
            Assert.IsTrue(TestEqual(o, r), $"{o} did not deserialize to the same value");
        }

        [TestMethod]
        public void Primitives()
        {
            TestRoundTrip(1);
            TestRoundTrip(1f);
            TestRoundTrip("foo");
            TestRoundTrip("\"foo\"");
            TestRoundTrip("\\");
        }

        [TestMethod]
        public void StateVariables()
        {
            TestRoundTrip(StateVariableName.Named("Foo"));
        }

        [TestMethod]
        public void Dictionaries()
        {
            TestRoundTrip(State.Empty);
            TestRoundTrip(State.Empty.Bind(StateVariableName.Named("Foo"), 10));
            TestRoundTrip(State.Empty.Bind(StateVariableName.Named("Foo"), 10).Bind(StateVariableName.Named("Bar"), 20));
        }
    }
}