using Step.Interpreter;

namespace Step.Tests
{
    [TestClass()]
    public class ElNodeTests
    {
        [TestMethod()]
        public void Write()
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
        public void Delete()
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
            CollectionAssert.AreEqual(new[] { "/a/b", "/c!e" }, deleted.SortedContents);
            deleted = node.Delete("/", "f", "!", "h", "/", "j");
            CollectionAssert.AreEqual(new[] { "/a/b", "/c!e", "/f!h/i" }, deleted.SortedContents);
            deleted = node.Delete("/", "f", "!", "h");
            CollectionAssert.AreEqual(new[] { "/a/b", "/c!e", "/f" }, deleted.SortedContents);
        }
    }
}