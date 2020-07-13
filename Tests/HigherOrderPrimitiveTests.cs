﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HigherOrderPrimitiveTests.cs" company="Ian Horswill">
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
    public class HigherOrderPrimitiveTests
    {
        [TestMethod]
        public void DoAllTest()
        {
            var m = new Module();
            m.AddDefinitions("Generate: a",
                "Generate: b",
                "Generate: c",
                "Test: [DoAll [Generate]]",
                "SecondTest: [DoAll [Generate] [Newline]]");
            Assert.AreEqual("A b c", m.Call("Test"));
            Assert.AreEqual("A\nB\nC\n", m.Call("SecondTest"));
        }

        [TestMethod]
        public void OnceTest()
        {
            var m = new Module();
            m.AddDefinitions("Generate: a",
                "Generate: b",
                "Generate: c",
                "Test: [DoAll [Once [Generate]]]");
            Assert.AreEqual("A", m.Call("Test"));
        }

        [TestMethod]
        public void ExactlyOnceTest()
        {
            var m = new Module();
            m.AddDefinitions("Generate: a",
                "Generate: b",
                "Generate: c",
                "Test: [DoAll [ExactlyOnce [Generate]]]");
            Assert.AreEqual("A", m.Call("Test"));
        }

        [TestMethod, ExpectedException(typeof(CallFailedException))]
        public void ExactlyOnceFailureTest()
        {
            var m = new Module();
            m.AddDefinitions("Generate: a",
                "Generate: b",
                "Generate: c",
                "Test: [DoAll [ExactlyOnce [Fail]]]");
            Assert.AreEqual("A", m.Call("Test"));
        }

        [TestMethod]
        public void HigherOrderUserCodeTest()
        {
            var m = new Module();
            m.AddDefinitions("Generate a:",
                "Generate b:",
                "Generate c:",
                "TryWrite ?x: ?x",
                "TryWrite ?x: oops!",
                "OnceForEach ?generator ?writer: [DoAll ?generator [ExactlyOnce ?writer]]",
                "Test: [OnceForEach [Generate ?x] [TryWrite ?x]]");
            Assert.AreEqual("A b c", m.Call("Test"));
        }

        [TestMethod]
        public void MaxTest()
        {
            var m = new Module();
            m.AddDefinitions("Generate a 1:",
                "Generate b 2:",
                "Generate c 1:",
                "Test: [Max ?score [Generate ?x ?score]] ?x ?score");
            Assert.AreEqual("B 2", m.Call("Test"));
        }

        [TestMethod]
        public void MinTest()
        {
            var m = new Module();
            m.AddDefinitions("Generate a 1:",
                "Generate b 0:",
                "Generate c 2:",
                "Test: [Min ?score [Generate ?x ?score]] ?x ?score");
            Assert.AreEqual("B 0", m.Call("Test"));
        }

        [TestMethod]
        public void MaxFailTest()
        {
            var m = new Module();
            m.AddDefinitions("Test: [Max ?score [Fail]] ?x ?score",
                "Test: Max failed");
            Assert.AreEqual("Max failed", m.Call("Test"));
        }
    }
}