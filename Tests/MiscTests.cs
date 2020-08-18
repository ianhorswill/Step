using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class MiscTests
    {
        [TestMethod]
        public void VerbConjugationTest()
        {
            var m = TestUtils.Module("Test 1: He eat[s], [set ThirdPersonSingular false] they eat[s].",
                "Test 2: He cry[es], [set ThirdPersonSingular false] they cry[es].",
            "Test 3: He buzz[es], [set ThirdPersonSingular false] they buzz[es].");
            Assert.AreEqual("He eats, they eat.", m.Call("Test", 1));
            Assert.AreEqual("He cries, they cry.", m.Call("Test" ,2));
            Assert.AreEqual("He buzzes, they buzz.", m.Call("Test", 3));
        }

        [TestMethod]
        public void InitializationTest()
        {
            var m = TestUtils.Module("Initially: [set X 1]");
            Assert.AreEqual(1, (int)m["X"]);
        }
    }
}
