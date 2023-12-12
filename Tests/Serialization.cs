using System.Collections;
using Step;
using Step.Interpreter;
using Step.Serialization;

namespace Tests
{
    #nullable enable
    [TestClass]
    public class Serialization
    {
        private void AssertTreeEqual(object a, object b)
        {
            if (a is State sa && b is State sb)
            {
                void AssertSubset(State x, State y)
                {
                    foreach (var pair in x.Bindings)
                        AssertTreeEqual(pair.Value!, y[pair.Key]!);
                }

                AssertSubset(sa, sb);
                AssertSubset(sb, sa);
                return;
            }

            if (a is object[] ta && b is object[] tb)
            {
                Assert.AreEqual(ta.Length, tb.Length);
                for (var i = 0; i < ta.Length; i++)
                    AssertTreeEqual(ta[i], tb[i]);
                return;
            }

            if (a is IDictionary ad && b is IDictionary bd)
            {
                var aBindings = new List<DictionaryEntry>();
                foreach (DictionaryEntry e in ad) aBindings.Add(e);
                var bBindings = new List<DictionaryEntry>();
                foreach (DictionaryEntry e in bd) bBindings.Add(e);

                object? Lookup(object? key, List<DictionaryEntry> bindings)
                    => bindings.First(e => Term.Comparer.Default.Equals(key, e.Key)).Value;

                foreach (DictionaryEntry e in ad)
                    AssertTreeEqual(Lookup(e.Key, bBindings)!, e.Value!);
                foreach (DictionaryEntry e in bd)
                    AssertTreeEqual(Lookup(e.Key, aBindings)!, e.Value!);
                return;
            }
            Assert.AreEqual(a, b);
        }

        private void TestRoundTrip(object? o, Module? m = null)
        {
            var serialized = Serializer.SerializeToString(o!);
            var r = Deserializer.Deserialize(new StringReader(serialized), m!);
            AssertTreeEqual(o!, r!);
        }

        [TestMethod]
        public void Primitives()
        {
            TestRoundTrip(1);
            TestRoundTrip(1f);
            TestRoundTrip("foo");
            TestRoundTrip("\"foo\"");
            TestRoundTrip("\\");
            TestRoundTrip(true);
            TestRoundTrip(false);
            TestRoundTrip(null);
            TestRoundTrip(Module.Global["Write"]!, Module.Global!);
        }

        [TestMethod]
        public void StateVariables()
        {
            TestRoundTrip(StateVariableName.Named("Foo"));
        }

        [TestMethod]
        public void GlobalVariables()
        {
            TestRoundTrip(State.Empty);
            TestRoundTrip(State.Empty.Bind(StateVariableName.Named("Foo"), 10));
            TestRoundTrip(State.Empty.Bind(StateVariableName.Named("Foo"), 10).Bind(StateVariableName.Named("Bar"), 20));
        }

        [TestMethod]
        public void Tuples()
        {
            TestRoundTrip(Array.Empty<object>());
            TestRoundTrip(new object[] { "foo", 1, new object[] { "bar" }});
        }

        [TestMethod]
        public void FluentState()
        {
            var m = Module.FromDefinitions("fluent Foo ?x.", "Test: [now [Foo 5]]");
            var (_, s) = m.Call(State.Empty, "Test");
            TestRoundTrip(s, m);
        }
    }
}