using Microsoft.VisualStudio.TestTools.UnitTesting;
using Step;
using Step.Interpreter;

namespace Tests
{
    [TestClass]
    public class TuplePredicateTests
    {
        private static readonly TuplePredicate Predicate = new TuplePredicate(
            "TP",
            new[] {typeof(string), typeof(string)},
            new [] {true, true},
            new[]
            {
                new object[] {"a", "a"},
                new object[] {"a", "b"},
                new object[] {"b", "b"},
                new object[] {"c", "c"}
            });
        
        [TestMethod, ExpectedException(typeof(ArgumentCountException))]
        public void ArgumentCountExceptionTest()
        {
            Module.Global["TP"] = Predicate;
            var m = Module.FromDefinitions("Test: [TP a]");

            m.CallPredicate("Test");
        }

        [TestMethod, ExpectedException(typeof(ArgumentTypeException))]
        public void ArgumentTypeExceptionTest()
        {
            Module.Global["TP"] = Predicate;
            var m = Module.FromDefinitions("Test: [TP a 1]");

            m.CallPredicate("Test");
        }

        [TestMethod]
        public void MatchTest()
        {
            Module.Global["TP"] = Predicate;
            var m = Module.FromDefinitions("[predicate] Test ?x ?y: [TP ?x ?y]");
            
            Assert.IsTrue(m.CallPredicate("Test", "a", "a"));
            Assert.IsTrue(m.CallPredicate("Test", "a", "b"));
            Assert.IsFalse(m.CallPredicate("Test", "a", "c"));
            Assert.IsFalse(m.CallPredicate("Test", "z", "z"));

            Assert.AreEqual("b", m.CallFunction<string>("Test", "b"));
        }
    }
}
