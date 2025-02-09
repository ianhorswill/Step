using System.Collections;
using Step.Interpreter;

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
    }
}