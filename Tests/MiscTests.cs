using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Step;

namespace Tests
{
    [TestClass]
    public class MiscTests
    {
        [TestMethod]
        public void VerbConjugationTest()
        {
            var m = TestUtils.Module("Test 1: [singular] [thirdPerson] He eat[s], [plural] [thirdPerson] they eat[s].",
                "Test 2: [singular] [thirdPerson] He cry[es], [plural][thirdPerson] they cry[es].",
            "Test 3: [singular] [thirdPerson] He buzz[es], [plural][thirdPerson] they buzz[es].",
                "Test 4: [singular] [thirdPerson] He [is] a cat, [plural][thirdPerson] they [is] cats.",
            "Test 5: [singular] [thirdPerson] He [has] a cat, [plural][thirdPerson] they [has] a cat.");
            Assert.AreEqual("He eats, they eat.", m.Call("Test", 1));
            Assert.AreEqual("He cries, they cry.", m.Call("Test" ,2));
            Assert.AreEqual("He buzzes, they buzz.", m.Call("Test", 3));
            Assert.AreEqual("He is a cat, they are cats.", m.Call("Test", 4));
            Assert.AreEqual("He has a cat, they have a cat.", m.Call("Test", 5));
        }

        [TestMethod]
        public void InitializationTest()
        {
            var m = TestUtils.Module("initially: [set X = 1]");
            Assert.AreEqual(1, (int)m["X"]);
        }

        [TestMethod]
        public void SingletonVariableTest()
        {
            var m = Module.FromDefinitions("[main] Test ?x.");
            Assert.AreEqual(1, m.Warnings().Count(s => s.Contains("used only once")));

            m = Module.FromDefinitions("[main] Test ?.");
            Assert.AreEqual(0, m.Warnings().Count(s => s.Contains("used only once")));

            m = Module.FromDefinitions("[main] Test ?_singleton.");
            Assert.AreEqual(0, m.Warnings().Count(s => s.Contains("used only once")));

            m = Module.FromDefinitions("[main] Test ?x: [Write ?x]");
            Assert.AreEqual(0, m.Warnings().Count(s => s.Contains("used only once")));
            
            m = Module.FromDefinitions("[main] Test ?x: ?x");
            Assert.AreEqual(0, m.Warnings().Count(s => s.Contains("used only once")));
        }

        [TestMethod]
        public void BrokenProjectTest()
        {
            //var m = new Module("Broken project");
            //// Uncomment to test loading a project that causes a problem
            //m.LoadDirectory("C:/users/ianho/documents/github/iHunt");
            //m.Call("Run");
        }

        [TestMethod, ExpectedException(typeof(StepTaskTimeoutException))]
        public void TimeoutException()
        {
            var m = Module.FromDefinitions("Test: [Test]");
            Module.SearchLimit = 50;
            try
            {
                m.Call("Test");
            }
            finally
            {
                // Let the other tests run normally.
                Module.SearchLimit = 0;
            }
        }
    }
}
