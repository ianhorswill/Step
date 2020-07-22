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
            var m = TestUtils.Module("Test: He eat[s], [set ThirdPersonSingular false] they eat[s].");
            Assert.AreEqual("He eats, they eat.", m.Call("Test"));
        }
    }
}
