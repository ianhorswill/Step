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
using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Step;
using Step.Utilities;

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

        //[TestMethod]
        //public void NthTest()
        //{
        //    var m = Module.FromDefinitions("Test1 ?l ?x: [Nth ?l ?x foo]",
        //        "Test2 ?l ?x: [Nth ?l 2 ?x]");

        //    var list = new object[] { 0, 1, "foo", 3, 4 };
        //    var badList = new object[] { 0, 1, 2, 3, 4 };
        //    Assert.AreEqual(2, m.CallFunction<int>("Test1", new [] {list} ));
        //    Assert.IsFalse(m.CallPredicate("Test1", badList, 2));

        //    Assert.AreEqual("foo", m.CallFunction<string>("Test2", new [] { list }));
        //    Assert.IsFalse(m.CallPredicate("Test2", list, "bar"));
        //}

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
            m.AddDefinitions("Test: [X] [set X = 2] [X]");
            Assert.AreEqual("1 2", m.Call("Test"));
        }

        [TestMethod]
        public void SetTest2()
        {
            var m = new Module("test") {["X"] = 1};
            m.AddDefinitions("Test ?x: [X] [set X = ?x] [X]");
            Assert.AreEqual("1 5", m.Call("Test", 5));
        }

        [TestMethod]
        public void SetArithmeticTest()
        {
            var m = new Module("test") { ["X"] = 1 };
            m.AddDefinitions("Test ?x: [set X = X+?x] [Write X]");
            Assert.AreEqual("6", m.Call("Test", 5));
        }
        
        [TestMethod]
        public void SetFloatTest()
        {
            var m = new Module("test") { ["X"] = 1 };
            m.AddDefinitions("Test: [set X = X + 1.5] [Write X]");
            Assert.AreEqual("2.5", m.Call("Test"));
        }

        [TestMethod]
        public void SetLocalTest()
        {
            var m = new Module("test");
            m.AddDefinitions("SucceedTest: [set ?x = 4 / 2] [Write ?x]");
            m.AddDefinitions("[fallible] FailTest: [= ?x 1] [set ?x = 2] [Write ?x]");
            Assert.AreEqual("2", m.Call("SucceedTest"));
            Assert.IsFalse(m.CallPredicate("FailTest"));
        }

        [TestMethod]
        public void AddRemoveTest()
        {
            var m = Module.FromDefinitions(
                "Test: [set X = Empty] [Push 1] [Push 2] [Push 3] [Pop] [Pop] [Pop]",
                "Push ?x: [add ?x X]",
                "[generator] Pop: [removeNext ?x X] [Write ?x]",
                "[fallible] FailTest: [set X = Empty] [Pop]");
            Assert.AreEqual("3 2 1", m.Call("Test"));
            Assert.IsFalse(m.CallPredicate(State.Empty, "FailTest"));
        }

        [TestMethod]
        public void MinQueueTest()
        {
            var m = Module.FromDefinitions(
                "Test1: [set Queue = EmptyMinQueue] [add [a 2] Queue] [add [b 1] Queue] [add [c 3] Queue] [removeNext ?x Queue] ?x [removeNext ?y Queue] ?y [removeNext ?z Queue] ?z",
                "[predicate] Test2: [removeNext ? EmptyMinQueue]"
            );

            Assert.AreEqual("B a c", m.Call("Test1"));
            Assert.IsFalse(m.CallPredicate("Test2"));
        }

        [TestMethod]
        public void MaxQueueTest()
        {
            var m = Module.FromDefinitions(
                "Test1: [set Queue = EmptyMaxQueue] [add [a 2] Queue] [add [b 1] Queue] [add [c 3] Queue] [removeNext ?x Queue] ?x [removeNext ?y Queue] ?y [removeNext ?z Queue] ?z",
                "[predicate] Test2: [removeNext ? EmptyMaxQueue]"
            );

            Assert.AreEqual("C a b", m.Call("Test1"));
            Assert.IsFalse(m.CallPredicate("Test2"));
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
            m.AddDefinitions("Test: [set List = empty] [PrintList] [add 1 List] [PrintList] [add 2 List] [PrintList]",
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
                "Test: [set List = empty] [Add 1] [Add 2] [Add 3]");
            Assert.AreEqual("1 2 1 3 2 1", m.Call("Test"));
        }

        [TestMethod]
        public void CountAttemptsTest()
        {
            var m = Module.FromDefinitions("Test: [CountAttempts ?a] [Write ?a] [= ?a 10] done");
            Assert.AreEqual("10 done", m.Call("Test"));
        }

        [TestMethod]
        public void WriteTests()
        {
            var m = Module.FromDefinitions("TestWriteString: [Write s]",
                "TestWriteNumber: [Write 103]",
                "TestWriteWithoutUnderscoresA: [Write a]",
                "TestWriteWithoutUnderscoresB: [Write a_b]",
                "TestWriteCapitalizedA: A [WriteCapitalized cat]",
                "TestWriteCapitalizedB: A [WriteCapitalized tabby_cat]",
                "TestWriteQuoted: [Write \"The taco run\"]",
                "TestWriteQuotedIndirect: [set X = \"The taco run\"] [Write X]");
            Assert.AreEqual("S", m.Call("TestWriteString"));
            Assert.AreEqual("103", m.Call("TestWriteNumber"));
            Assert.AreEqual("A", m.Call("TestWriteWithoutUnderscoresA"));
            Assert.AreEqual("A b", m.Call("TestWriteWithoutUnderscoresB"));
            Assert.AreEqual("A Cat", m.Call("TestWriteCapitalizedA"));
            Assert.AreEqual("A Tabby cat", m.Call("TestWriteCapitalizedB"));
            Assert.AreEqual("The taco run", m.Call("TestWriteQuoted"));
            Assert.AreEqual("The taco run", m.Call("TestWriteQuotedIndirect"));
        }

        [TestMethod]
        public void MakeHashtableTest()
        {
            var m = Module.FromDefinitions("Test ?out: [Hashtable a 1 b 2 c 3 ?out]");
            var h = m.CallFunction<Hashtable>("Test");
            Assert.AreEqual(1, h["a"]);
            Assert.AreEqual(2, h["b"]);
            Assert.AreEqual(3, h["c"]);
        }

        [TestMethod]
        public void HelpTest()
        {
            var m = Module.FromDefinitions("Test: [Help =]");
            Assert.AreEqual("[= a b]\nMatches (unifies) a and b, and succeeds when they're the same.", m.Call("Test"));
        }

        [TestMethod]
        public void MakeManualTest()
        {
            Documentation.WriteHtmlReference(Module.Global, "manual.htm");
            Assert.IsTrue(true);
        }

    //    [TestMethod]
    //    public void AproposTest()
    //    {
    //        var m = Module.FromDefinitions("Test: [Apropos unif]");
    //        Assert.AreEqual(@"[= a b]
    //Matches (unifies) a and b, and succeeds when they're the same.
    
    //[PreviousCall ?call_pattern]
    //Unifies ?call_pattern with the most recent successful call that matches it.  Backtracking will match against previous calls.
    
    //[UniqueCall reflection ?call_pattern]
    //Calls ?call_pattern, finding successive solutions until one is found that can't be unified with a previous successful call.
    
    //", m.Call("Test"));
    //    }
    }
}
