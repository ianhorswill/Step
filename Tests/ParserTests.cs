#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ParserTests.cs" company="Ian Horswill">
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

using System.IO;
using System.Linq;
using Step;
using Step.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void TokenStreamTest()
        {
            void Test(string input, int expectedLength, string output)
            {
                if (output == null)
                    output = input;
                var tokens = new TokenStream(new StringReader(input), null).Tokens.ToArray();
                Assert.AreEqual(expectedLength, tokens.Length);
                Assert.AreEqual(output, tokens.Untokenize(false));
            }

            Test("this is a test", 4, null);
            Test(" this is a test ", 4, "this is a test");
            Test("     ", 0, "");
            Test("", 0, "");
            Test("[]", 2, "[]");
            Test(" [ ] ", 2, "[]");
            Test("[a]", 3, "[a]");
        }

        [TestMethod]
        public void ExpressionStreamPassthroughTest()
        {
            void Test(string input, int expectedLength, string output)
            {
                if (output == null)
                    output = input;
                var tokens = new ExpressionStream(new StringReader(input), null).Expressions.Cast<string>().ToArray();
                Assert.AreEqual(expectedLength, tokens.Length);
                Assert.AreEqual(output, tokens.Untokenize(false));
            }

            Test("this is a test", 4, null);
            Test(" this is a test ", 4, "this is a test");
            Test("     ", 0, "");
            Test("", 0, "");
        }

        [TestMethod]
        public void ExpressionStreamExpressionTest()
        {
            void Test(string input, params object[] expectedResult)
            {
                var expressions = new ExpressionStream(new StringReader(input), null).Expressions.ToArray();
                Assert.AreEqual(expectedResult.Length, expressions.Length, "Output is of unexpected length");
                for (var i = 0; i < expectedResult.Length; i++)
                {
                    var expected = expectedResult[i];
                    var actual = expressions[i];
                    if (expected is object[] array)
                    {
                        if (actual is object[] actualArray)
                        {
                            Assert.AreEqual(array.Length, actualArray.Length, $"Output expression is unexpected length at position {i}");
                            for (var j = 0; j < array.Length; j++)
                                Assert.AreEqual(array[j], actualArray[j], $"Mismatch in output at position {i}, element number {j}");
                        } else
                            Assert.Fail($"Output at position {i} should be an array but instead is {actual}");
                    } else
                        Assert.AreEqual(expected,actual, $"output mismatch at position {i}");
                }
            }

            Test("this is a test", "this", "is", "a", "test");
            Test(" [a] b [c] d", new object[] {"a"}, "b", new object[] {"c"}, "d");
            Test("b [c] d [a]", "b", new object[] {"c"}, "d", new object[] {"a"});
            Test("");
        }

        [TestMethod, ExpectedException(typeof(SyntaxError))]
        public void PrematureEofErrorTest()
        {
            void Test(string input, params object[] expectedResult)
            {
                var expressions = new ExpressionStream(new StringReader(input), null).Expressions.ToArray();
                Assert.AreEqual(expectedResult.Length, expressions.Length, "Output is of unexpected length");
                for (var i = 0; i < expectedResult.Length; i++)
                {
                    var expected = expectedResult[i];
                    var actual = expressions[i];
                    if (expected is object[] array)
                    {
                        if (actual is object[] actualArray)
                        {
                            Assert.AreEqual(array.Length, actualArray.Length, $"Output expression is unexpected length at position {i}");
                            for (var j = 0; j < array.Length; j++)
                                Assert.AreEqual(array[j], actualArray[j], $"Mismatch in output at position {i}, element number {j}");
                        } else
                            Assert.Fail($"Output at position {i} should be an array but instead is {actual}");
                    } else
                        Assert.AreEqual(expected,actual, $"output mismatch at position {i}");
                }
            }

            Test("a [a b c d", new object[] {"a"}, "b", new object[] {"c"}, "d");
        }

        [TestMethod, ExpectedException(typeof(SyntaxError))]
        public void NestingErrorTest()
        {
            void Test(string input, params object[] expectedResult)
            {
                var expressions = new ExpressionStream(new StringReader(input), null).Expressions.ToArray();
                Assert.AreEqual(expectedResult.Length, expressions.Length, "Output is of unexpected length");
                for (var i = 0; i < expectedResult.Length; i++)
                {
                    var expected = expectedResult[i];
                    var actual = expressions[i];
                    if (expected is object[] array)
                    {
                        if (actual is object[] actualArray)
                        {
                            Assert.AreEqual(array.Length, actualArray.Length, $"Output expression is unexpected length at position {i}");
                            for (var j = 0; j < array.Length; j++)
                                Assert.AreEqual(array[j], actualArray[j], $"Mismatch in output at position {i}, element number {j}");
                        } else
                            Assert.Fail($"Output at position {i} should be an array but instead is {actual}");
                    } else
                        Assert.AreEqual(expected,actual, $"output mismatch at position {i}");
                }
            }

            Test("a [a [b] c d", new object[] {"a"}, "b", new object[] {"c"}, "d");
        }

        [TestMethod, ExpectedException(typeof(SyntaxError))]
        public void StrayCloseBracketErrorTest()
        {
            void Test(string input, params object[] expectedResult)
            {
                var expressions = new ExpressionStream(new StringReader(input), null).Expressions.ToArray();
                Assert.AreEqual(expectedResult.Length, expressions.Length, "Output is of unexpected length");
                for (var i = 0; i < expectedResult.Length; i++)
                {
                    var expected = expectedResult[i];
                    var actual = expressions[i];
                    if (expected is object[] array)
                    {
                        if (actual is object[] actualArray)
                        {
                            Assert.AreEqual(array.Length, actualArray.Length, $"Output expression is unexpected length at position {i}");
                            for (var j = 0; j < array.Length; j++)
                                Assert.AreEqual(array[j], actualArray[j], $"Mismatch in output at position {i}, element number {j}");
                        } else
                            Assert.Fail($"Output at position {i} should be an array but instead is {actual}");
                    } else
                        Assert.AreEqual(expected,actual, $"output mismatch at position {i}");
                }
            }

            Test("a a b] c d", new object[] {"a"}, "b", new object[] {"c"}, "d");
        }

        [TestMethod]
        public void LoadModuleTest()
        {
            var m = Module.FromDefinitions("Test: this is a test");
            Assert.AreEqual("This is a test",m.Call("Test"));
        }

        [TestMethod]
        public void ComplexTest()
        {
            var m = Module.FromDefinitions(
                "Self subject: I",
                "Self object: me",
                "Self reflexive: myself",
                "Test: [Self subject] like [Self reflexive]");
            Assert.AreEqual("I like myself",m.Call("Test"));
        }

        [TestMethod]
        public void MultiDefinitionTest()
        {
            var m = Module.FromDefinitions(
                "Self subject: I\nSelf object: me\nSelf reflexive: myself",
                "Test: [Self subject] like [Self reflexive]");
            Assert.AreEqual("I like myself",m.Call("Test"));
        }

        [TestMethod]
        public void HigherOrderTest()
        {
            var m = Module.FromDefinitions(@"Speaker subject: I
Speaker object: me
Listener subject: you
Listener object: you
Love ?x ?y: [?x subject] love [?y object]
Test: [Love Speaker Listener]");
            Assert.AreEqual("I love you",m.Call("Test"));
        }

        [TestMethod]
        public void MultiLineDefinitionTest()
        {
            var m = Module.FromDefinitions(@"
Multi:
This is a test of
a very nice feature
of multi-line definitions

With a line break in it!
[end]

Test: [Multi]
");
            Assert.AreEqual("This is a test of a very nice feature of multi-line definitions\nWith a line break in it!",
                m.Call("Test"));
        }
    }
}
