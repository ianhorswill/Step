#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EmitTest.cs" company="Ian Horswill">
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
using Step.Output;

namespace Tests
{
    [TestClass]
    public class EmitTest
    {
        [TestMethod]
        public void Emit1()
        {
            var s = new EmitStep(new [] { "this", "is", "a", "test" }, null);
            Assert.AreEqual("This is a test", s.Expand());
        }

        [TestMethod]
        public void Emit2()
        {
            var s = TestUtils.Sequence(new [] { "this", "is" }, new [] {"a", "test" });
            Assert.AreEqual("This is a test", s.Expand());
        }

        [TestMethod]
        public void CapitalizationTest()
        {
            var tokens = new []
            {
                "this", "is", "a", "test", ".",
                "and", "this", "is", "a", "test", "."
            };

            Assert.AreEqual(
                "This is a test.  And this is a test.",
                tokens.Untokenize());
            Assert.AreEqual(
                "this is a test.  and this is a test.",
                tokens.Untokenize(new FormattingOptions() { Capitalize = false }));
            Assert.AreEqual(
                "this is a test. and this is a test.",
                tokens.Untokenize(new FormattingOptions() { Capitalize = false, FrenchSpacing = false }));
        }

        [TestMethod]
        public void AAnTest()
        {
            var m = Module.FromDefinitions("Test: [an] cat [a] cat [an] ox [a] ox");
            Assert.AreEqual("A cat a cat an ox an ox", m.Call("Test"));
        }

        [TestMethod]
        public void ForceSpaceTest()
        {
            var m = Module.FromDefinitions(
                "TestNormal: a [ForceSpace] b",
                "TestNoBreak: :-",
                "TestBreak: : [ForceSpace] -");
            
            Assert.AreEqual("A b", m.Call("TestNormal"));
            Assert.AreEqual(":-", m.Call("TestNoBreak"));
            Assert.AreEqual(": -", m.Call("TestBreak"));
        }

        [TestMethod]
        public void SuppressVerticalSpaceTest()
        {
            var m = Module.FromDefinitions("Test: [Paragraph] [NewLine] A [Paragraph] B.");
            Assert.AreEqual("A\n\nB.", m.Call("Test"));
        }
    }
}
