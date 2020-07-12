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
            var m = new Module();
            m.AddDefinitions("Test: [Throw a b c]");
            m.Call("Test");
        }

        [TestMethod]
        public void StringFormTest()
        {
            var m = new Module();
            m.AddDefinitions("Test: [StringForm 123 ?x] ?x");
            Assert.AreEqual("123",m.Call("Test"));
        }

        [TestMethod]
        public void MentionHookTest()
        {
            var m = new Module();
            m.AddDefinitions("Test: [StringForm 123 ?x] ?x",
                "MentionHook ?x: foo");
            Assert.AreEqual("Foo",m.Call("Test"));
        }

        [TestMethod]
        public void SetTest()
        {
            var m = new Module {["X"] = 1};
            m.AddDefinitions("Test: [X] [Set X 2] [X]");
            Assert.AreEqual("1 2", m.Call("Test"));
        }

        [TestMethod]
        public void SetTest2()
        {
            var m = new Module {["X"] = 1};
            m.AddDefinitions("Test ?x: [X] [Set X ?x] [X]");
            Assert.AreEqual("1 5", m.Call("Test", 5));
        }

        [TestMethod]
        public void EqualsTest()
        {
            var m = new Module();
            m.AddDefinitions("Test ?x ?y: [= ?x ?z] [= ?z ?y] succeeded",
                "Testx ?x: [Test ?x ?y] [= ?y 1] Succeeded");
            Assert.AreEqual("Succeeded", m.Call("Test", 1, 1));
            Assert.AreEqual(null, m.Call("Test", 1, 2));
            
            Assert.AreEqual("Succeeded Succeeded", m.Call("Testx", 1));
            Assert.AreEqual(null, m.Call("Testx", 2));
        }

        [TestMethod]
        public void LessThanTest()
        {
            var m = new Module();
            m.AddDefinitions("Test ?x ?y: [< ?x ?y] Succeeded");
            Assert.AreEqual("Succeeded", m.Call("Test", 1, 2));
            Assert.AreEqual(null, m.Call("Test", 1, 1));
        }

        [TestMethod]
        public void ErrorPrintingTest()
        {
            var m = new Module();
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
            Assert.AreEqual("Wrong number of arguments for <, expected 2, got 3: [< \"=\" 1 2]", result);
        }
    }
}
