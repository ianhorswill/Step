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
using Step.Interpreter;
using Step.Output;

namespace Tests
{
    [TestClass]
    public class ParserTests
    {
        [TestMethod]
        public void LoadStepFileTest()
        {
            var m = new Module(nameof(LoadStepFileTest));
            m.LoadDefinitions("../../../Test.step");
            Assert.AreEqual("Test", m.Call("Test"));
        }

        [TestMethod]
        public void LoadCsvFileTest()
        {
            var m = new Module(nameof(LoadCsvFileTest));
            m.LoadDefinitions("../../../Test.csv");
            Assert.IsTrue(m.CallPredicate("Test", "a", "b"));
        }

        [TestMethod]
        public void LocalsInTuplesTest()
        {
            // ReSharper disable StringLiteralTypo
            var warnings = Module.FromDefinitions("[main] Subquest [DefeatHenchpeep ?H ?Hench ?A] [DefeatAntagonist ?H ?A]: [Henchpeep ?Hench ?A]")
                // ReSharper restore StringLiteralTypo
                // ReSharper disable once StringLiteralTypo
                .Warnings().Where(w => w.Contains("ingleton")).ToArray();
            Assert.AreEqual(0, warnings.Length);
        }
        
        [TestMethod]
        public void DeclarationTests()
        {
            var m = Module.FromDefinitions("task Foo.\npredicate Bar ?.");
            Assert.IsInstanceOfType(m["Foo"], typeof(CompoundTask));
            Assert.AreEqual(CompoundTask.TaskFlags.None, ((CompoundTask)m["Foo"]).Flags);
            Assert.IsInstanceOfType(m["Bar"], typeof(CompoundTask));
            Assert.AreEqual(CompoundTask.TaskFlags.Fallible | CompoundTask.TaskFlags.MultipleSolutions,
                ((CompoundTask)m["Bar"]).Flags);
            
            m = Module.FromDefinitions("[randomly] task Foo.\n[randomly] predicate Bar ?.");
            Assert.IsInstanceOfType(m["Foo"], typeof(CompoundTask));
            Assert.AreEqual(CompoundTask.TaskFlags.Shuffle, ((CompoundTask)m["Foo"]).Flags);
            Assert.IsInstanceOfType(m["Bar"], typeof(CompoundTask));
            Assert.AreEqual(CompoundTask.TaskFlags.Shuffle | CompoundTask.TaskFlags.Fallible | CompoundTask.TaskFlags.MultipleSolutions,
                ((CompoundTask)m["Bar"]).Flags);
        }

        [TestMethod]
        public void ParseWeightTest()
        {
            var m = Module.FromDefinitions("[10.5] Test ?x.", "[2] Test a.", "Test ?: foo", "[randomly] [1] Foo.", "[randomly]\n[1]\nFoo.");
            var test = (CompoundTask) m["Test"];
            Assert.AreEqual(10.5f, test.Methods[0].Weight);
            Assert.AreEqual(2, test.Methods[1].Weight);
            Assert.AreEqual(1, test.Methods[2].Weight);
        }
        
        [TestMethod]
        public void TokenStreamTest()
        {
            void Test(string input, int expectedLength, string output)
            {
                if (output == null)
                    output = input;
                var tokens = new TextFileTokenStream(new StringReader(input), null).Tokens.ToArray();
                Assert.AreEqual(expectedLength, tokens.Length);
                Assert.AreEqual(output, tokens.Untokenize(new FormattingOptions() { Capitalize = false }));
            }

            Test("\"Foo,\" I said.  \"I don't like this.\"", 16, "\u201cFoo,\u201d I said.  \u201cI don't like this.\u201d");
            Test("This <i>is also</i> a test", 7, "This<i> is also</i> a test");
            Test("this is a test", 4, null);
            Test(" this is a test ", 4, "this is a test");
            Test("     ", 0, "");
            Test("", 0, "");
            Test("[]", 2, "[]");
            Test(" [ ] ", 2, "[]");
            Test("[a]", 3, "[a]");
            Test(">=", 1, ">=");
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
                Assert.AreEqual(output, tokens.Untokenize(new FormattingOptions() { Capitalize = false }));
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
        public void IncompleteDefinitionTest()
        {
            Module.FromDefinitions("Test ?a [foo ?a]");
        }

        [TestMethod]
        public void ParseFactTest()
        {
            Module.FromDefinitions(
                @"[randomly] 
Compatibility ? ? [sharedInterest lolcats].
Compatibility ? ? [sharedInterest [love dearLeader]].
Compatibility ? ? [sharedInterest [hate negativePeople]].

MentionTuple [sharedInterest [love dearLeader]]: their deep and abiding love of Dear Leader
MentionTuple [sharedInterest [hate negativePeople]]: how they both hate people with negative energy
MentionTuple [sharedInterest ?what]: their shared interest in ?what

[randomly]
Incompatibility ?a ?b [unsharedInterest2 ?i]: [Interest ?a ?i] [Not [Interest ?b ?i]]
Incompatibility ?a ?b [unsharedInterest1 ?i]: [Not [Interest ?a ?i]] [Interest ?b ?i]
Incompatibility ?a ?b [incompatibleTraits ?a ?b ?ta ?tb]: [Trait ?a ?ta] [Trait ?b ?tb]
Incompatibility ?a ?b [GenericIncompatibility ?g].

MentionTuple [unsharedInterest1 ?i]: their lack of interest in ?i
MentionTuple [unsharedInterest2 ?i]: their annoying obsession with ?i
MentionTuple [incompatibleTraits ?a ?b ?ta ?tb]: ?a being ?ta

[randomly]
GenericIncompatibility toothpaste.
GenericIncompatibility toiletPaper.
GenericIncompatibility snoring.");
        }

        [TestMethod, ExpectedException(typeof(SyntaxError))]
        public void ParseBadFactTest()
        {
            Module.FromDefinitions(
                @"[randomly] 
Compatibility ? ? [sharedInterest lolcats]:
Compatibility ? ? [sharedInterest [love dearLeader]]:
Compatibility ? ? [sharedInterest [hate negativePeople]]:

MentionTuple [sharedInterest [love dearLeader]]: their deep and abiding love of Dear Leader
MentionTuple [sharedInterest [hate negativePeople]]: how they both hate people with negative energy
MentionTuple [sharedInterest ?what]: their shared interest in ?what

[randomly]
Incompatibility ?a ?b [unsharedInterest2 ?i]: [Interest ?a ?i] [Not [Interest ?b ?i]]
Incompatibility ?a ?b [unsharedInterest1 ?i]: [Not [Interest ?a ?i]] [Interest ?b ?i]
Incompatibility ?a ?b [incompatibleTraits ?a ?b ?ta ?tb]: [Trait ?a ?ta] [Trait ?b ?tb]
Incompatibility ?a ?b [GenericIncompatibility ?g].

MentionTuple [unsharedInterest1 ?i]: their lack of interest in ?i
MentionTuple [unsharedInterest2 ?i]: their annoying obsession with ?i
MentionTuple [incompatibleTraits ?a ?b ?ta ?tb]: ?a being ?ta

[randomly]
GenericIncompatibility toothpaste.
GenericIncompatibility toiletPaper.
GenericIncompatibility snoring.");
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
        public void ParseNumberTest()
        {
            var m = Module.FromDefinitions(
                "PositiveInteger ?x: [= ?x 10]",
                "NegativeInteger ?x: [= ?x -10]",
                "PositiveFloat ?x: [= ?x 10.5]",
                "NegativeFloat ?x: [= ?x -10.5]");
            
            Assert.AreEqual(10, m.CallFunction<int>("PositiveInteger"));
            Assert.AreEqual(-10, m.CallFunction<int>("NegativeInteger"));
            Assert.AreEqual(10.5, m.CallFunction<float>("PositiveFloat"));
            Assert.AreEqual(-10.5, m.CallFunction<float>("NegativeFloat"));
        }

        [TestMethod]
        public void LoadModuleTest()
        {
            var m = Module.FromDefinitions("Test: this is a test");
            Assert.AreEqual("This is a test",m.Call("Test"));
        }

        [TestMethod]
        public void QuotedTextTest()
        {
            var m = Module.FromDefinitions("Test: [Write \"This is a test\"]",
                "Foo \"this is a string in a head\".",
                "Test2: [Foo ?x] ?x/Write",
                "Test3: [Write \":\"]");
            Assert.AreEqual("This is a test", m.Call("Test"));
            Assert.AreEqual("This is a string in a head", m.Call("Test2"));
            Assert.AreEqual(":", m.Call("Test3"));
        }

        [TestMethod]
        public void QuotedStringTest()
        {
            var m = Module.FromDefinitions("Test: [Write |Foo|]");
            Assert.AreEqual("Foo", m.Call("Test"));
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

With a paragraph break in it!
[end]

Test: [Multi]
");
            Assert.AreEqual("This is a test of a very nice feature of multi-line definitions\n\nWith a paragraph break in it!",
                m.Call("Test"));
        }

        [TestMethod]
        public void CommentTest()
        {
            var m = Module.FromDefinitions(@"#A comment at start of line
   # An indented comment
   Test: foo bar [Baz] # a comment at end of line
Baz: baz
# Comment ends with eof",
                @"task Foo ?x.
#comment at end
",
                "Test2: [Write |#|]");
            Assert.AreEqual("Foo bar baz", m.Call("Test"));
            Assert.AreEqual("#", m.Call("Test2"));
        }

        [TestMethod]
        public void CommentTest2()
        {
            var m = Module.FromDefinitions(@"Test:
a
#foo
#bar
b
[end]");
            Assert.AreEqual("A b", m.Call("Test"));
        }

        [TestMethod]
        public void TokenizeCommentTest()
        {
            var actual = TextFileTokenStream.Tokenize(@"Foo:
a
#foo
#bar
b
[end]");
            var expected = new[] {"Foo", ":", "\n", "a", "\n", "\n", "\n", "b", "\n", "[", "end", "]"};
            Assert.AreEqual(actual.Length, expected.Length);
            for (var i = 0; i < actual.Length; i++)
                Assert.AreEqual(expected[i], actual[i]);
        }

        [TestMethod]
        public void TokenizeCommentTest2()
        {
            var actual = TextFileTokenStream.Tokenize(@"Test: foo  # comment
Baz: baz");
            var expected = new[] { "Test", ":", "foo", "\n", "Baz", ":", "baz" };
            Assert.AreEqual(actual.Length, expected.Length);
            for (var i = 0; i < actual.Length; i++)
                Assert.AreEqual(expected[i], actual[i]);
        }

        [TestMethod]
        public void InlineLocalVariableTest()
        {
            var m = Module.FromDefinitions("Test ?x: ?x ?x/Foo ?x/Bar/Foo",
                "Foo ?x: [Write ?x]",
                "Bar a b.");
            Assert.AreEqual("A a b", m.Call("Test", "a"));
        }

        [TestMethod]
        public void InlineGlobalVariableTest()
        {
            var m = Module.FromDefinitions("Test: [set X = a] ^X ^X/Foo ^X/Bar/Foo",
                "Foo ?x: [Write ?x]",
                "Bar a b.");
            Assert.AreEqual("A a b", m.Call("Test"));
        }
    }
}
