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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Step;
using Step.Interpreter;

namespace Tests
{
    [TestClass]
    public class CallTests
    {
        private PrimitiveTask.Predicate1 positive = (n) => (int) n > 0;
        private PrimitiveTask.DeterministicTextGenerator1 toString => (x) => new []{ x.ToString() };

        bool Succeeds(Step.Interpreter.Step s)
        {
            return s.Try(PartialOutput.NewEmpty(), BindingEnvironment.NewEmpty(), (o, e, ds) => true);
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
            var s = TestUtils.Sequence(new object[] {new object[] {toString, 1}});
            Assert.AreEqual("1", s.Expand());
        }
    }
}
