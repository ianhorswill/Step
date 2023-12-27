using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Step.Output;

namespace Step.Interpreter
{
    internal abstract class ElNode
    {
        public static readonly ElNode Empty = new NonExclusive();

        public ElNode? Write(ElNode? child, object?[] path, int position) 
            => (child == null) ? Build(path, position) : child.Write(path, position);

        public ElNode Write(params object?[] path) => Write(path, 0);

        protected abstract ElNode Write(object?[] path, int position);

        public ElNode? Delete(params object?[] path)
        {
            if (path.Length % 2 != 0)
                throw new ArgumentException($"Attempting to delete path with an odd length");
            return Delete(path, 1);
        }
        protected ElNode? Delete(object?[] path, int position)
        {
            var key = path[position];
            if (position == path.Length - 1)
                return DeleteKey(key);
            var child = ChildOf(key);
            if (child == null)
                return this;  // the string doesn't appear in the database, so we don't have to do any work to delete it.
            return Replace(key, child.Delete(path, position + 2));
        }

        protected ElNode? Build(object?[] path, int position)
        {
            switch (path.Length - position)
            {
                case 0:
                    return null;

                case 1:
                    return new Leaf(path[position]);

                default:
                    var child = Build(path, position + 2);
                    switch (path[position])
                    {
                        case "/":
                            return new NonExclusive(path[position + 1], child);

                        case "!":
                            return new Exclusive(path[position + 1], child);

                        default:
                            throw new ArgumentException($"Invalid exclusion logic path separator {path[position]}");
                    }
            }
        }

        protected abstract ElNode Replace(object? key, ElNode? child);

        protected abstract ElNode? ChildOf(object? key);

        protected abstract ElNode? DeleteKey(object? key);

        public abstract IEnumerable<string> Contents { get;  }

        public string[] SortedContents
        {
            get
            {
                var c= Contents.ToArray();
                Array.Sort(c);
                return c;
            }
        }

        private sealed class NonExclusive: ElNode
        {
            private NonExclusive(ImmutableDictionary<object, ElNode?> children)
            {
                Children = children;
            }

            public NonExclusive() : this(ImmutableDictionary<object, ElNode?>.Empty)
            { }

            public readonly ImmutableDictionary<object, ElNode?> Children;

            public NonExclusive(object? key, ElNode? child) : this(ImmutableDictionary<object,ElNode?>.Empty.Add(key,child))
            { }

            protected override ElNode Replace(object? key, ElNode? child) =>
                Children.TryGetValue(key, out var oldChild) && child == oldChild
                    ? this
                    : new NonExclusive(Children.SetItem(key, child));

            protected override ElNode? ChildOf(object? key)
                => Children.TryGetValue(key, out var child) ? child : null;

            protected override ElNode? DeleteKey(object? key)
            {
                if (Children.ContainsKey(key!))
                    return new NonExclusive(Children.Remove(key!));
                return this;
            }

            protected override ElNode Write(object?[] path, int position)
            {
                if (position > path.Length -2)
                    return this;

                ValidateSeparator(path, position);

                var key= path[position+1];
                if (key == null)
                    throw new ArgumentException("Null cannot be used as an element of an exclusion logic statement.");
                return Replace(key, Write(ChildOf(key), path, position + 2));
            }

            private static void ValidateSeparator(object?[] path, int position)
            {
                switch (path[position])
                {
                    case "/":
                        break;
                    case "!":
                        throw new ArgumentException("Switching an element of an exclusion logic string from / to !");
                    default:
                        throw new ArgumentException($"Invalid exclusion logic path separator {path[position]}");
                }
            }

            public override IEnumerable<string> Contents
            {
                get
                {
                    foreach (var pair in Children)
                    {
                        var prefix = "/" + pair.Key.ToTermString();
                        var child = pair.Value;
                        if (child == null)
                            yield return prefix;
                        else
                            foreach (var sentence in child.Contents)
                                yield return prefix + sentence;
                    }
                }
            }
        }

        private sealed class Exclusive : ElNode
        {
            public readonly object? Key;
            public readonly ElNode? Child;
            public Exclusive(object? key, ElNode? child)
            {
                Key = key;
                Child = child;
            }

            protected override ElNode Replace(object? key, ElNode? child)
            {
                if (key == Key && child == Child) return this;
                return new Exclusive(key, child);
            }

            protected override ElNode? ChildOf(object? key)
                => (Key == key) ? Child : null;

            protected override ElNode? DeleteKey(object? key)
            {
                return (Key == key) ? null : this;
            }

            protected override ElNode Write(object?[] path, int position)
            {
                switch (path.Length - position)
                {
                    case 0:
                    case 1:
                        return this;

                    case 2:
                        if (path[position] == null || !path[position]!.Equals("!"))
                            throw new ArgumentException($"Expected ! in exclusion logic path but got {path[position]}");
                        var k = path[position+1];
                        if (k == Key) return this;
                        return new Exclusive(k, Child);

                    default:
                        var newChild = Write(Child, path, position + 2);
                        var newKey = path[position+1];
                        return Replace(newKey, newChild);
                }
            }

            public override IEnumerable<string> Contents
            {
                get
                {
                    var prefix = "!" + Key.ToTermString();
                    if (Child == null)
                        yield return prefix;
                    else
                        foreach (var sentence in Child.Contents)
                            yield return prefix + sentence;
                }
            }
        }

        private sealed class Leaf : ElNode
        {
            public readonly object? Key;

            public Leaf(object? key)
            {
                Key = key;
            }

            protected override ElNode Write(object?[] path, int position)
            {
                switch (path.Length - position)
                {
                    case 0:
                    case 1:
                        return this;

                    case 2:
                        if (path[position + 1] == Key)
                            return this;
                        return new Leaf(path[position + 1]);

                    default:
                        return Build(path, position);
                }
            }

            public override IEnumerable<string> Contents => new[] { Key.ToTermString() };

            protected override ElNode Replace(object key, ElNode? child)
            {
                throw new NotImplementedException();
            }

            protected override ElNode? DeleteKey(object? key)
            {
                return (Key == key) ? null : this;
            }

            protected override ElNode? ChildOf(object key)
            {
                throw new NotImplementedException();
            }
        }
    }
}
