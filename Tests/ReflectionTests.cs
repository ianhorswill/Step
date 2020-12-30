using Microsoft.VisualStudio.TestTools.UnitTesting;
using Step;

namespace Tests
{
    [TestClass]
    public class ReflectionTests
    {
        [TestMethod]
        public void CompoundTaskTest()
        {
            var m = Module.FromDefinitions(
                "Test: [ForEach [CompoundTask ?t] [Write ?t]]",
                "A.",
                "B: [A]");
            Assert.AreEqual("Test A B", m.Call("Test"));
        }

        [TestMethod]
        public void MethodReificationTest()
        {
            var m = Module.FromDefinitions(
                "InOutTest: [ForEach [TaskMethod A ?m] [Write ?m]]",
                "InInTest: [TaskMethod A ?m] [TaskMethod A ?m]",             // The first call is InOut, but the second is InIn
                "OutInTest: [TaskMethod A ?m] [TaskMethod ?t ?m] [= A ?t]",  // first is InOut, second is OutIn
                "A 1.",
                "A 2.");
            Assert.AreEqual("Method [A 1] Method [A 2]", m.Call("InOutTest"));
            Assert.IsTrue(m.CallPredicate(State.Empty, "InInTest"));
            Assert.IsTrue(m.CallPredicate(State.Empty, "OutInTest"));
        }

        [TestMethod]
        public void CallerChainTest()
        {
            var m = Module.FromDefinitions(
                "Test: [A] [B] [C] [LastMethodCallFrame ?f] [ForEach [CallerChainAncestor ?f ?method] [Write ?method]]",
                "A: [C] [D]",
                "B: [C]",
                "C.",
                "D.");
            Assert.AreEqual("Method [C] Method [Test]", m.Call("Test"));
        }

        [TestMethod]
        public void GoalChainTest()
        {
            var m = Module.FromDefinitions(
                "Test: [A] [B] [C] [LastMethodCallFrame ?f] [ForEach [GoalChainAncestor ?f ?method] [Write ?method]]",
                "A: [C] [D]",
                "B: [C]",
                "C.",
                "D.");
            Assert.AreEqual("Method [C] Method [C] Method [B] Method [D] Method [C] Method [A] Method [Test]", m.Call("Test"));
        }

        [TestMethod]
        public void LintUnusedTaskTest()
        {
            var m = Module.FromDefinitions(
                "[main] Test: [A] [B] [C] [LastMethodCallFrame ?f] [ForEach [GoalChainAncestor ?f ?method] [Write ?method]]",
                "Unused: [Fail]",
                "A: [C] [D]",
                "B: [C]",
                "C.",
                "D.");
            Assert.AreEqual("Unused is defined but never called.",
                string.Join("\n", m.Warnings()));
        }

        [TestMethod]
        public void TaskCallsTests()
        {
            var m = Module.FromDefinitions(
                "TestInInSucceed: [TaskCalls Test A]",
                "TestInInFail: [TaskCalls A Test]",
                "TestInOut: [ForEach [TaskCalls A ?callee] [Write ?callee]]",
                "TestOutIn: [ForEach [TaskCalls ?caller C] [Write ?caller]]",
                "TestOutOut: [ForEach [Begin [TaskCalls ?caller ?callee] [CompoundTask ?callee]] [Write ?caller] [Write ?callee] [Write ,]]",
                "UncalledTask ?t: [CompoundTask ?t] [Not [TaskCalls ? ?t]]",
                "TestUncalled: [ForEach [UncalledTask ?t] [Write ?t]]",
                "[main] Test: [A] [B] [C] [LastMethodCallFrame ?f] [ForEach [GoalChainAncestor ?f ?method] [Write ?method]]",
                "Unused: [Fail]",
                "A: [C] [D]",
                "B: [C]",
                "C.",
                "D.");
            Assert.IsTrue(m.CallPredicate("TestInInSucceed"));
            Assert.IsFalse(m.CallPredicate("TestInInFail"));
            Assert.AreEqual("C D", m.Call("TestInOut"));
            Assert.AreEqual("Test A B", m.Call("TestOutIn"));
            Assert.AreEqual("Test A, Test B, Test C, A C, A D, B C,", m.Call("TestOutOut"));
            Assert.AreEqual("TestInInSucceed TestInInFail TestInOut TestOutIn TestOutOut UncalledTask TestUncalled Test Unused", m.Call("TestUncalled"));
        }
    }
}
