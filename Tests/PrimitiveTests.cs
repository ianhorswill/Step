#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrimitiveTests.cs" company="Ian Horswill">
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

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Step;

namespace Tests
{
    [TestClass]
    public class PrimitiveTests
    {
        [TestMethod, ExpectedException(typeof(Exception))]
        public void ThrowTest()
        {
            var m = new Module("test");
            m.AddDefinitions("Test: [Throw a b c]");
            m.Call("Test");
        }

        [TestMethod]
        public void StringFormTest()
        {
            var m = new Module("test");
            m.AddDefinitions("Test: [StringForm 123 ?x] ?x",
                "Mention ?x: [Write ?x]");
            Assert.AreEqual("123",m.Call("Test"));
        }

        [TestMethod]
        public void MentionTest()
        {
            var m = new Module("test");
            m.AddDefinitions("Test: [StringForm 123 ?x] ?x",
                "Mention ?x: foo");
            Assert.AreEqual("Foo",m.Call("Test"));
        }

        [TestMethod]
        public void SetTest()
        {
            var m = new Module("test") {["X"] = 1};
            m.AddDefinitions("Test: [X] [set X 2] [X]");
            Assert.AreEqual("1 2", m.Call("Test"));
        }

        [TestMethod]
        public void SetTest2()
        {
            var m = new Module("test") {["X"] = 1};
            m.AddDefinitions("Test ?x: [X] [set X ?x] [X]");
            Assert.AreEqual("1 5", m.Call("Test", 5));
        }

        [TestMethod]
        public void AddRemoveTest()
        {
            var m = Module.FromDefinitions(
                "Test: [set X Empty] [Push 1] [Push 2] [Push 3] [Pop] [Pop] [Pop]",
                "Push ?x: [add ?x X]",
                "[generator] Pop: [removeNext ?x X] [Write ?x]",
                "[fallible] FailTest: [set X Empty] [Pop]");
            Assert.AreEqual("3 2 1", m.Call("Test"));
            Assert.IsFalse(m.CallPredicate(State.Empty, "FailTest"));
        }

        [TestMethod]
        public void EqualsTest()
        {
            var m = new Module("test");
            m.AddDefinitions("[fallible] Test ?x ?y: [= ?x ?z] [= ?z ?y] succeeded",
                "[fallible] TextX ?x: [Test ?x ?y] [= ?y 1] Succeeded");
            Assert.AreEqual("Succeeded", m.Call("Test", 1, 1));
            Assert.AreEqual(null, m.Call("Test", 1, 2));
            
            Assert.AreEqual("Succeeded Succeeded", m.Call("TextX", 1));
            Assert.AreEqual(null, m.Call("TextX", 2));
        }

        [TestMethod]
        public void LessThanTest()
        {
            var m = new Module("test");
            m.AddDefinitions("[fallible] Test ?x ?y: [< ?x ?y] Succeeded");
            Assert.AreEqual("Succeeded", m.Call("Test", 1, 2));
            Assert.AreEqual(null, m.Call("Test", 1, 1));
        }

        [TestMethod]
        public void ErrorPrintingTest()
        {
            var m = new Module("test");
            m.AddDefinitions("Test ?x ?y: [< = ?x ?y] Succeeded");
            var result = "";
            try
            {
                m.Call("Test", 1, 2);
            }
            catch (Exception e)
            {
                result = e.Message;
            }
            Assert.AreEqual("Wrong number of arguments for <, expected 2, got 3: [< = 1 2]", result);
        }

        [TestMethod]
        public void ListTest()
        {
            var m = new Module("test");
            m.AddDefinitions("Test: [set List empty] [PrintList] [add 1 List] [PrintList] [add 2 List] [PrintList]",
                "PrintList: [DoAll [Member ?e List] [Write ?e]]",
                "[fallible] TestEmpty: [Member 1 Empty]");
            Assert.AreEqual("1 2 1", m.Call("Test"));
            Assert.IsFalse(m.CallPredicate("TestEmpty"));
        }

        [TestMethod]
        public void ListTest2()
        {
            var m = new Module("test");
            m.AddDefinitions("Add ?x: [add ?x List] [PrintList]",
                "PrintList: [DoAll [Member ?e List] [Write ?e]]",
                "Test: [set List empty] [Add 1] [Add 2] [Add 3]");
            Assert.AreEqual("1 2 1 3 2 1", m.Call("Test"));
        }

        [TestMethod]
        public void CountAttemptsTest()
        {
            var m = Module.FromDefinitions("Test: [CountAttempts ?a] [Write ?a] [= ?a 10] done");
            Assert.AreEqual("10 done", m.Call("Test"));
        }
    }
}
