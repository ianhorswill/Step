using System.Net.Mail;
using Step.Interpreter;

namespace Step.Tests
{
    [TestClass()]
    public class ElNodeTests
    {
        [TestMethod()]
        public void LowLevelWrite()
        {
            var node = ElNode.Empty;
            node = node.Write("/", "a");
            CollectionAssert.AreEqual(new[] { "/a" }, node.SortedContents);
            node = node.Write("/", "a", "/", "b");
            CollectionAssert.AreEqual(new[] { "/a/b" }, node.SortedContents);
            node = node.Write("/", "c", "!", "d");
            CollectionAssert.AreEqual(new[] { "/a/b", "/c!d" }, node.SortedContents);
            node = node.Write("/", "c", "!", "e");
            CollectionAssert.AreEqual(new[] { "/a/b", "/c!e" }, node.SortedContents);
            node = node.Write("/", "f");
            CollectionAssert.AreEqual(new[] { "/a/b", "/c!e", "/f" }, node.SortedContents);
            node = node.Write("/", "f", "!", "g" );
            CollectionAssert.AreEqual(new[] { "/a/b", "/c!e", "/f!g" }, node.SortedContents);
            node = node.Write("/", "f", "!", "h" );
            CollectionAssert.AreEqual(new[] { "/a/b", "/c!e", "/f!h" }, node.SortedContents);
            node = node.Write("/", "f", "!", "h", "/", "i" );
            CollectionAssert.AreEqual(new[] { "/a/b", "/c!e", "/f!h/i" }, node.SortedContents);
            node = node.Write("/", "f", "!", "h", "/", "j" );
            CollectionAssert.AreEqual(new[] { "/a/b", "/c!e", "/f!h/i", "/f!h/j" }, node.SortedContents);
        }

        [TestMethod()]
        public void LowLevelDelete()
        {
            var node = ElNode.Empty;
            node = node.Write("/", "a");
            node = node.Write("/", "a", "/", "b");
            node = node.Write("/", "c", "!", "d");
            node = node.Write("/", "c", "!", "e");
            node = node.Write("/", "f");
            node = node.Write("/", "f", "!", "g" );
            node = node.Write("/", "f", "!", "h" );
            node = node.Write("/", "f", "!", "h", "/", "i" );
            node = node.Write("/", "f", "!", "h", "/", "j" );
            CollectionAssert.AreEqual(new[] { "/a/b", "/c!e", "/f!h/i", "/f!h/j" }, node.SortedContents);
            var deleted = node.Delete("/", "f");
            CollectionAssert.AreEqual(new[] { "/a/b", "/c!e" }, deleted!.SortedContents);
            deleted = node.Delete("/", "f", "!", "h", "/", "j");
            CollectionAssert.AreEqual(new[] { "/a/b", "/c!e", "/f!h/i" }, deleted!.SortedContents);
            deleted = node.Delete("/", "f", "!", "h");
            CollectionAssert.AreEqual(new[] { "/a/b", "/c!e", "/f" }, deleted!.SortedContents);
        }

        [TestMethod]
        public void LowLevelArrays()
        {
            var node = ElNode.Empty;
            node = node.Write("/", "a");
            node = node.Write("/", "a", "/", "b");
            node = node.Write("/", "a", "/", new object[] { "a", "b" });
            node = node.Write("/", "a", "/", new object[] { "a", "b" });
            node = node.Write("/", "a", "/", new object[] { "a", "b" }, "/", "c");
            node = node.Write("/", "a", "/", new object[] { "a", "b" }, "/", "d");
            node = node.Write("/", "c", "!", "d");
            node = node.Write("/", "c", "!", "e");
            node = node.Write("/", "f");
            node = node.Write("/", "f", "!", "g" );
            node = node.Write("/", "f", "!", "h" );
            node = node.Write("/", "f", "!", "h", "/", "i" );
            node = node.Write("/", "f", "!", "h", "/", "j" );
            CollectionAssert.AreEqual(new[] { "/a/[a b]/c", "/a/[a b]/d", "/a/b", "/c!e", "/f!h/i", "/f!h/j" }, node.SortedContents);
            var deleted = node.Delete("/", "a", "/", new object[] { "a", "b" });
            CollectionAssert.AreEqual(new[] { "/a/b", "/c!e", "/f!h/i", "/f!h/j" }, deleted!.SortedContents);
        }

        [TestMethod]
        public void ElStore()
        {
            var m = Module.FromDefinitions("Test: [ElStore [/a/b!c]]");
            var result = m.Call(State.Empty,"Test");
            var el = (ElNode)result.newDynamicState[ElNode.ElState];
            CollectionAssert.AreEqual(new [] { "/a/b!c" }, el.SortedContents);
        }

        [TestMethod]
        public void Lookup()
        {
            var m = Module.FromDefinitions("Test: [/c!d]");
            var node = ElNode.Empty
                    .Write("/", "a")
                    .Write("/", "a", "/", "b")
                    .Write("/", "a", "/", new object[] { "a", "b" })
                    .Write("/", "a", "/", new object[] { "a", "b" })
                    .Write("/", "a", "/", new object[] { "a", "b" }, "/", "c")
                    .Write("/", "a", "/", new object[] { "a", "b" }, "/", "d")
                    .Write("/", "c", "!", "d")
                    .Write("/", "f")
                    .Write("/", "f", "!", "g" )
                    .Write("/", "f", "!", "h" )
                    .Write("/", "f", "!", "h", "/", "i" )
                    .Write("/", "f", "!", "h", "/", "j" );
            var state = State.Empty.Bind(ElNode.ElState, node);
            
            var result = m.Call(state,"Test");
        }

        [TestMethod]
        public void Bind()
        {
            var m = Module.FromDefinitions("Test: [DoAll [/?x] [Write ?x]]",
                "Test2: [DoAll [/?x/[?x b]/] [Write ?x]]");
            var node = ElNode.Empty
                .Write("/", "a")
                .Write("/", "a", "/", "b")
                .Write("/", "a", "/", new object[] { "a", "b" })
                .Write("/", "a", "/", new object[] { "a", "b" })
                .Write("/", "a", "/", new object[] { "a", "b" }, "/", "c")
                .Write("/", "a", "/", new object[] { "a", "b" }, "/", "d")
                .Write("/", "c", "!", "d")
                .Write("/", "f")
                .Write("/", "f", "!", "g" )
                .Write("/", "f", "!", "h" )
                .Write("/", "f", "!", "h", "/", "i" )
                .Write("/", "f", "!", "h", "/", "j" );
            var state = State.Empty.Bind(ElNode.ElState, node);
            
            var result = m.Call(state,"Test");
            
            var matches = result.output.ToLower().Split();
            Array.Sort(matches);

            CollectionAssert.AreEqual(new [] { "a", "c", "f" }, matches);
            Assert.AreEqual("A", m.Call(state, "Test2").output);
        }

        [TestMethod]
        public void ElDelete()
        {
            var m = Module.FromDefinitions("Test: [ElDelete [/a]]");
            var node = ElNode.Empty
                .Write("/", "a")
                .Write("/", "a", "/", "b")
                .Write("/", "a", "/", new object[] { "a", "b" })
                .Write("/", "a", "/", new object[] { "a", "b" })
                .Write("/", "a", "/", new object[] { "a", "b" }, "/", "c")
                .Write("/", "a", "/", new object[] { "a", "b" }, "/", "d")
                .Write("/", "c", "!", "d")
                .Write("/", "f")
                .Write("/", "f", "!", "g" )
                .Write("/", "f", "!", "h" )
                .Write("/", "f", "!", "h", "/", "i" )
                .Write("/", "f", "!", "h", "/", "j" );
            var state = State.Empty.Bind(ElNode.ElState, node);
            
            var deleted = (ElNode)m.Call(state,"Test").newDynamicState[ElNode.ElState];
           
            CollectionAssert.AreEqual(new[] { "/c!d", "/f!h/i", "/f!h/j" }, deleted.SortedContents);
        }
    }
}