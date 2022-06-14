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
            var m = TestUtils.Module("Test 1: He eat[s], [set ThirdPersonSingular = false] they eat[s].",
                "Test 2: He cry[es], [set ThirdPersonSingular = false] they cry[es].",
            "Test 3: He buzz[es], [set ThirdPersonSingular = false] they buzz[es].");
            Assert.AreEqual("He eats, they eat.", m.Call("Test", 1));
            Assert.AreEqual("He cries, they cry.", m.Call("Test" ,2));
            Assert.AreEqual("He buzzes, they buzz.", m.Call("Test", 3));
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
            var m = new Module("Broken project");
            // Uncomment to test loading a project that causes a problem
            m.LoadDirectory("C:/users/ianho/documents/github/ihunt");
        }
    }
}
