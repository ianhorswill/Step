#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CallTests.cs" company="Ian Horswill">
// Copyright (C) 2020 Ian Horswill
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion

using System.Collections;
using Step;
using Step.Interpreter;

namespace Tests
{
    [TestClass]
    public class CallTests
    {
        private readonly SimplePredicate<int> positive = new SimplePredicate<int>("Positive", n => n > 0);
        private static readonly DeterministicTextGenerator<object> ToStringPrimitive = new DeterministicTextGenerator<object>("ToString", (x) => new []{ x.ToString() });

        bool Succeeds(Step.Interpreter.Step s)
        {
            return s.Try(TextBuffer.NewEmpty(), BindingEnvironment.NewEmpty(), (o, e, ds, p) => true, null);
        }

        [TestMethod]
        public void Predicate1TestTrue()
        {
            var s = TestUtils.Sequence(new object[] {new object[] {positive, 1}});
            Assert.IsTrue(Succeeds(s));
        }

        [TestMethod]
        public void Predicate1TestFalse()
        {
            var s = TestUtils.Sequence(new object[] {new object[] {positive, -1}});
            Assert.IsFalse(Succeeds(s));
        }

        [TestMethod]
        public void DeterministicGenerator1Test()
        {
            var s = TestUtils.Sequence(new object[] {new object[] {ToStringPrimitive, 1}});
            Assert.AreEqual("1", s.Expand());
        }

        [TestMethod]
        public void MatchGlobalTest()
        {
            var m = new Module("test") {["X"] = 1};
            m.AddDefinitions("Test X: hit",
                "Test ?x: miss");
            Assert.AreEqual("Hit",m.Call("Test", 1));
            Assert.AreEqual("Miss",m.Call("Test", 2));
        }

        [TestMethod]
        public void InlineCallTest()
        {
            var m = new Module("test");
            m.AddDefinitions("Inline ?x: inline",
                "Method ?x: ?x/Inline",
                "Test: [Method 1]");
            Assert.AreEqual("Inline",m.Call("Test"));
        }

        [TestMethod]
        public void PathTest()
        {
            var m = new Module("test");
            m.AddDefinitions("Map 1 2:",
                "Map 2 3:",
                "Method ?x: ?x/Map/Write",
                "Test: [Method 1]");
            Assert.AreEqual("2",m.Call("Test"));
        }

        [TestMethod]
        public void PathTest2()
        {
            var m = new Module("test");
            m.AddDefinitions("Map 1 2:",
                "Map 2 3:",
                "Method ?x: ?x/Map",
                "Mention ?x: [Write ?x]",
                "Test: [Method 1]");
            Assert.AreEqual("2", m.Call("Test"));
        }

        [TestMethod]
        public void PathPlusTest()
        {
            var m = new Module("test");
            m.AddDefinitions("Map 1 2:",
                "Map 2 3:",
                "Foo ?x: foo",
                "Method ?x: ?x/Map/Write+Foo",
                "Test: [Method 1]");
            Assert.AreEqual("2 foo",m.Call("Test"));
        }

        [TestMethod]
        public void StackTraceTest()
        {
            var m = new Module("test");
            m.AddDefinitions("Map 1 2:",
                "Map 2 3:",
                "Foo ?x: foo",
                "Method ?x: ?x/Map/Write+Foo",
                "Test: [Method 1]");
            m.Call("Test");
            Assert.AreEqual("[Foo 2][Method 1][Test]",
                Module.StackTrace().Replace("\n", "").Replace("\r", ""));
        }

        [TestMethod, ExpectedException(typeof(StackOverflowException))]
        public void StackOverflowTest()
        {
            var m = Module.FromDefinitions("Test: [Test]");
            MethodCallFrame.MaxStackDepth = 100;
            m.Call("Test");
        }

        [TestMethod]
        public void DynamicTestTrace()
        {
            var m = Module.FromDefinitions(
                "Test: [A] [B] [C]",
                "A: [C] [D]",
                "B: [C]",
                "C.",
                "D.");
            m.Call("Test");
            Assert.AreEqual("C C B D C A Test",
                string.Join(" ",
                    MethodCallFrame.GoalChain(MethodCallFrame.CurrentFrame).Select(f => f.Method.Task.Name)));
        }

        [TestMethod]
        public void ListCallTest()
        {
            var m = Module.FromDefinitions("Test: [ForEach [Objects ?x] [Write ?x]]");
            m["Objects"] = new Cons(1, new Cons(2, new Cons(3, Cons.Empty)));
            Assert.AreEqual("1 2 3", m.Call("Test"));
        }

        [TestMethod]
        public void CallPredicateTest()
        {
            var m = Module.FromDefinitions("[fallible] Test (Number ?x):");
            Assert.IsTrue(m.CallPredicate(State.Empty, "Test", 1));
            Assert.IsFalse(m.CallPredicate(State.Empty, "Test", "test"));
        }

        [TestMethod]
        public void CallFunctionTest()
        {
            var m = Module.FromDefinitions("Test ?x ?x:");
            Assert.AreEqual(1, m.CallFunction<int>(State.Empty, "Test", 1));
        }

        [TestMethod]
        public void DestructuringCallTest()
        {
            var m = Module.FromDefinitions("Target [foo ?x]: [Write ?x]", "Test: [Target [foo 1]]");
            Assert.AreEqual("1", m.Call("Test"));
        }

        [TestMethod]
        public void StructureCreationTest()
        {
            var m = Module.FromDefinitions("Test ?x [foo ?x].",
                "[fallible] TestB: [Test 1 ?x] [Test 2 ?y] [= ?x ?y]");
            var result1 = m.CallFunction<object[]>(State.Empty, "Test", 1);
            var result2 = m.CallFunction<object[]>(State.Empty, "Test", 2);
            Assert.AreNotEqual(result1, result2);
            Assert.AreEqual("foo", result1[0]);
            Assert.AreEqual(1, result1[1]);
            Assert.AreEqual("foo", result2[0]);
            Assert.AreEqual(2, result2[1]);
            Assert.IsFalse(m.CallPredicate(State.Empty, "TestB"));
        }

        [TestMethod]
        public void StructureCreationStackTraceTest()
        {
            var m = Module.FromDefinitions("Test ?x [foo ?x].",
                "[fallible] TestB: [Test 1 ?x] [Test 2 ?y] [= ?x ?y]");
            m.CallFunction<object[]>(State.Empty, "Test", 1);
            Assert.AreEqual("[Test 1 [foo 1]]", Module.StackTrace().Trim());
        }

        [TestMethod]
        public void CallStructureBindingTest()
        {
            var m = Module.FromDefinitions("Rescue ?rescued ?saver [medicalSituation ?who ?x]: [Write ?rescued]");
            
            Assert.AreEqual("ManicPixieDreamPeep",
                m.ParseAndExecute("[Rescue manicPixieDreamPeep billionaire [medicalSituation manicPixieDreamPeep rareDisease]]"));
        }

        [TestMethod]
        public void ReadModeTest()
        {
            var m = Module.FromDefinitions(
                "TestSucceed: [Parse [SayTest] \"This is a test.\"]",
                "[fallible] TestFail: [Parse [SayTest] \"This is not a test.\"]",
                "TestMatch ?y: [Parse [SayTestMatch ?y] \"This is a test\"]",
                "TestMatchNumber ?y: [Parse [SayTestMatch ?y] \"This is a 5\"]",
                "[fallible] SayTest: This is a test.",
                "[fallible] SayTestMatch ?x: This is a ?x/Write");
            Assert.IsTrue(m.CallPredicate("TestSucceed"));
            Assert.IsFalse(m.CallPredicate("TestFail"));
            Assert.AreEqual("test", m.CallFunction<string>("TestMatch"));
            Assert.AreEqual(5, m.CallFunction<int>("TestMatchNumber"));
        }

        [TestMethod]
        // ReSharper disable once InconsistentNaming
        public void CFGParseTest()
        {
            var m = Module.FromDefinitions(
                "[generator] S: [NP] [VP]",
                "[generator] NP: [Det] [N]",
                "[generator] Det: the",
                "Det: a",
                "[generator] N: cat",
                "N: dog",
                "[generator] VP: [V] [NP]",
                "[generator] V: chased",
                "V: loved",
                "TestSucceed: [Parse [S] \"the cat loved the dog\"]",
                "[fallible] TestFail: [Parse [S] \"the cat bit the dog\"]");
            
            Assert.IsTrue(m.CallPredicate("TestSucceed"));
            Assert.IsFalse(m.CallPredicate("TestFail"));


        }

        [TestMethod]
        public void HashTableMatchTest()
        {
            var m = Module.FromDefinitions("Test: [Foo s 1] [Not [Foo s 2]]");
            m["Foo"] = new Hashtable() {{"s", 1}};
            Assert.IsTrue(m.CallPredicate("Test"));
        }
    }
}
