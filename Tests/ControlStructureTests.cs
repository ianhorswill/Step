using Microsoft.VisualStudio.TestTools.UnitTesting;
using Step;

namespace Tests
{
    [TestClass]
    public class ControlStructureTests
    {
        [TestMethod]
        public void CoolTest()
        {
            var m = new Module("test");
            m.AddDefinitions("TestCool: [firstOf] [cool] A [or] B [end]",
                "Test: [TestCool] [TestCool] [TestCool] [TestCool] ");
            Assert.AreEqual("A B A B", m.Call("Test"));
        }

        [TestMethod]
        public void CoolTest2()
        {
            var m = new Module("test");
            m.AddDefinitions("TestCool: [firstOf] [cool 2] A [or] B [end]",
                "Test: [TestCool] [TestCool] [TestCool] [TestCool] ");
            Assert.AreEqual("A B B A", m.Call("Test"));
        }

        [TestMethod]
        public void RandomCoolTest()
        {
            var m = TestUtils.Module(
                "Test: [randomly] [cool] sullen [or] [cool] drunk [or] [cool] vacant [or] [cool] lost [or] [cool] bored [or] crazed [end]");
            for (var i = 0; i < 100; i++)
                Assert.AreNotEqual(null, m.Call("Test"));
        }

        [TestMethod]
        public void OnceTest()
        {
            var m = new Module("test");
            m.AddDefinitions("TestCool: [firstOf] [once] A [or] B [end]",
                "Test: [TestCool] [TestCool] [TestCool] [TestCool] ");
            Assert.AreEqual("A B B B", m.Call("Test"));
        }

        [TestMethod]
        public void CaseTest()
        {
            var m = new Module("test");
            m.AddDefinitions("Test ?x: [case ?x] Number : number [or] String: string [else] other [end]");
            Assert.AreEqual("Number", m.Call("Test", 1));
            Assert.AreEqual("String", m.Call("Test", "foo"));
            Assert.AreEqual("Other", m.Call("Test", false));
        }

        [TestMethod]
        public void CaseTest2()
        {
            var m = new Module("test");
            m.AddDefinitions("Test ?x: [case ?x] [> 5] : big [else] small [end]");
            Assert.AreEqual("Small", m.Call("Test", 1));
            Assert.AreEqual("Big", m.Call("Test", 10));
        }

        [TestMethod]
        public void RandomlyTest()
        {
            var m = new Module("test");
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
            var m = new Module("test");
            m.AddDefinitions("Generate: [firstOf] a [or] b [else] c [end].",
                "Test: [DoAll [Generate]]");
            
            Assert.AreEqual("A.  B.  C.", m.Call("Test"));
        }

        [TestMethod]
        public void SequenceTest()
        {
            var m = new Module("test");
            m.AddDefinitions("[fallible]Generate: [sequence] a [then] b [then] c [end]",
                "[fallible]Test: [Generate] 1 [Generate] 2 [Generate] 3",
                "TestFail: [Test][Test]", "TestFail: Failed");

            Assert.AreEqual("A 1 b 2 c 3", m.Call("Test"));
            Assert.AreEqual("Failed", m.Call("TestFail"));
        }

        [TestMethod]
        public void EmptyElseTest()
        {
            var m = TestUtils.Module("Test ?x: [case ?x] Number: A [else] [end]");
            Assert.AreEqual("", m.Call("Test", "string"));
        }
    }
}