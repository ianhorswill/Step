namespace Step.Tests
{
    [TestClass()]
    public class FeatureStructureTests
    {
        [TestMethod()]
        public void HeadMatchingTest()
        {
            var m = Module.FromDefinitions("Test: [Match ?a {feature: ?b}] ?a ?b",
                "Match default {feature: 1}.");
            Assert.AreEqual("Default 1", m.Call("Test"));
        }

        [TestMethod]
        public void HeadLiftingTest()
        {
            var m = Module.FromDefinitions(
                "[predicate] Test1: [Match {feature: ?b}]",
                "[predicate] Test2: [Match {feature: 1}]",
                "[predicate] Match {feature: (+?x)}.");
            Assert.IsFalse(m.CallPredicate("Test1"));
            Assert.IsTrue(m.CallPredicate("Test2"));
        }

        [TestMethod]
        public void ParsingTest()
        {
            var m = Module.FromDefinitions(
                "Character { givenName: \"Diana\" familyName: \"Ratfliffe\" age: 34 occupation: \"tital of industry\" }.",
                "Recursive { foo: { givenName: \"Diana\" familyName: \"Ratfliffe\" age: 34 occupation: \"tital of industry\"} bar: 1}.");
        }
    }
}