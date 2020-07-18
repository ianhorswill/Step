using Microsoft.VisualStudio.TestTools.UnitTesting;
using Step;

namespace Tests
{
    [TestClass]
    public class ControlStructureTests
    {
        [TestMethod]
        public void CaseTest()
        {
            var m = new Module();
            m.AddDefinitions("Test ?x: [case ?x] Number : number [or] String: string [else] other [end]");
            Assert.AreEqual("Number", m.Call("Test", 1));
            Assert.AreEqual("String", m.Call("Test", "foo"));
            Assert.AreEqual("Other", m.Call("Test", false));
        }

        [TestMethod]
        public void CaseTest2()
        {
            var m = new Module();
            m.AddDefinitions("Test ?x: [case ?x] [> 5] : big [else] small [end]");
            Assert.AreEqual("Small", m.Call("Test", 1));
            Assert.AreEqual("Big", m.Call("Test", 10));
        }

        [TestMethod]
        public void RandomlyTest()
        {
            var m = new Module();
            m.AddDefinitions("Test: [randomly] a [or] b [else] c [end]");
            var gotA = 0;
            var gotB = 0;
            var gotC = 0;

            for (var i = 0; i < 100; i++)
            {
                var s = m.Call("Test");
                switch (s)
                {
                    case "A":
                        gotA++;
                        break;

                    case "B":
                        gotB++;
                        break;

                    case "C":
                        gotC++;
                        break;

                    default:
                        Assert.Fail($"Invalid result: {s}");
                        break;
                }
            }

            Assert.IsTrue(gotA > 0);
            Assert.IsTrue(gotB > 0);
            Assert.IsTrue(gotC > 0);
        }

        [TestMethod]
        public void FirstOfTest()
        {
            var m = new Module();
            m.AddDefinitions("Generate: [firstOf] a [or] b [else] c [end].",
                "Test: [DoAll [Generate]]");
            
            Assert.AreEqual("A.  B.  C.", m.Call("Test"));
        }
    }
}