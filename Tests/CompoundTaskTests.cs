#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundTaskTests.cs" company="Ian Horswill">
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Step;
using Step.Interpreter;

namespace Tests
{
    [TestClass]
    public class CompoundTaskTests
    {
        // ReSharper disable once InconsistentNaming
        private static readonly DeterministicTextGenerator<object> toString = new DeterministicTextGenerator<object>("ToString", (x) => new[] { x.ToString() });

        [TestMethod]
        public void MatchingNoVariablesTest()
        {
            var t = new CompoundTask("test", 1);
            t.Flags |= CompoundTask.TaskFlags.Fallible;
            t.AddMethod(1, new object[]{1}, new LocalVariableName[0], new EmitStep(new []{ "1", "matched"}, null), 0, null, 1);
            t.AddMethod(1, new object[]{2}, new LocalVariableName[0], new EmitStep(new []{ "2", "matched"}, null), 0, null, 1);

            Assert.AreEqual("1 matched", new Call(t, new object[]{1}, null).Expand());
            Assert.AreEqual("2 matched", new Call(t, new object[]{2}, null).Expand());
            Assert.AreEqual(null, new Call(t, new object[]{3}, null).Expand());
        }

        [TestMethod]
        public void DownwardUnifyTest1()
        {
            var t = new CompoundTask("test", 1);
            // ReSharper disable once InconsistentNaming
            var X = new LocalVariableName("X", 0);
            var locals = new[] { X };
            t.AddMethod(1, new object[] {X}, locals,
                TestUtils.Sequence(new object[] {toString, X}, new[] {"matched"}), 0,
                null, 1);

            Assert.AreEqual("1 matched", new Call(t, new object[]{1}, null).Expand());
            Assert.AreEqual("2 matched", new Call(t, new object[]{2}, null).Expand());
        }

        [TestMethod]
        public void UpwardUnifyTest1()
        {
            var up = new CompoundTask("up", 1);
            up.AddMethod(1, new object[] {"xyz"}, new LocalVariableName[0],
                null, 0, null, 1);

            var down = new CompoundTask("down", 1);
            // ReSharper disable once InconsistentNaming
            var X = new LocalVariableName("X", 0);
            down.AddMethod(1, new object[] {X}, new[] { X },
                TestUtils.Sequence(new object[] {toString, X}, new[] {"matched"}), 0,
                null, 1);

            var test = new CompoundTask("test", 0);
            // ReSharper disable once InconsistentNaming
            var Y = new LocalVariableName("Y", 0);
            test.AddMethod(1, new object[0], new [] { Y },
                TestUtils.Sequence(new object[] { up, Y }, new object[] { down, Y } ), 0,
                null, 1);

            Assert.AreEqual("Xyz matched", new Call(test, new object[0], null).Expand());
        }

        [TestMethod]
        public void UnifyNullTest()
        {
            var b = new BindingEnvironment();
            Assert.IsFalse(b.Unify(null, "foo", null, out _));
            Assert.IsTrue(b.Unify(null, null, null, out _));
        }

        [TestMethod]
        public void RandomlyTest()
        {
            var m = new Module("test");
            m.AddDefinitions("[randomly] Test: a", "Test: b", "Test: c ");
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
        public void WeightedRandomlyTest()
        {
            var m = new Module("test");
            m.AddDefinitions("[randomly] [9999999999] Test: a", "Test: b", "Test: c ");
            var gotA = 0;
            // ReSharper disable once NotAccessedVariable
            var gotB = 0;
            // ReSharper disable once NotAccessedVariable
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

            Assert.IsTrue(gotA > 99);
        }

        [TestMethod, ExpectedException(typeof(CallFailedException))]
        public void MustWorkTest()
        {
            TestUtils.Module("FailTest: [Fail]", "Test: [FailTest]", "Test: succeeded").Call("Test");
        }

        [TestMethod]
        public void FirstSuccessTest()
        {
            var m = TestUtils.Module("First: A", "First: B", "Test: [DoAll [First]]");
            Assert.AreEqual("A", m.Call("Test"));
        }
    }
}
