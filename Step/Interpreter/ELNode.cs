using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Step.Output;
using Step.Utilities;
#pragma warning disable CS8604

namespace Step.Interpreter
{
    internal abstract class ElNode
    {
        internal static readonly ElNode Empty = new NonExclusive();

        public static readonly StateElementWithDefault ElState = new StateElementWithDefault("ElKb", Empty);
        public static readonly GeneralPrimitive ElLookupPrimitive = new GeneralPrimitive("/", ElLookup);

        internal static void DefineGlobals()
        {
            Documentation.SectionIntroduction("exclusion logic",
                "Implementation of Richard Evans' exclusion logic, aka eremic logic.  Not that this implementation follows the syntactic conventions of UnityProlog, meaning that the non-exclusive concatenation operator is / and the exclusive one is !");
            Module.Global["/"] = ElLookupPrimitive.Arguments("sentence")
                .Documentation("exclusion logic",
                    "True if sentence unifies with at least one sentence in the KB.");
            Module.Global[nameof(ElStore)] = new GeneralPrimitive(nameof(ElStore), ElStore)
                .Arguments("sentence")
                .Documentation("exclusion logic",
                    "Adds sentence to KB.  If sentence contains ! operators then these will overwrite any existing data.");

            Module.Global[nameof(ElDelete)] = new GeneralPrimitive(nameof(ElDelete), ElDelete).Arguments("sentence")
                .Documentation("exclusion logic",
                    "Removes sentence and any sentences it is a prefix of from the KB.");
            Module.Global[nameof(ElDump)] = new GeneralPrimitive(nameof(ElDump), ElDump).Arguments()
                .Documentation("exclusion logic",
                    "Prints the complete contents of the exclusion logic KB.");
            }

        private static bool ElStore(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor,
            Step.Continuation k)
        {
            ArgumentCountException.Check(nameof(ElStore), 1, args);
            var path = ArgumentTypeException.Cast<object?[]>(nameof(ElStore), args[0], args);
            return k(o, e.Unifications,
                    e.State.Bind(ElState, ((ElNode)e.State[ElState]!).Write(path)),
                    predecessor);
        }

        private static bool ElDelete(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor,
            Step.Continuation k)
        {
            ArgumentCountException.Check(nameof(ElDelete), 1, args);
            var path = ArgumentTypeException.Cast<object?[]>(nameof(ElDelete), args[0], args);
            return k(o, e.Unifications,
                e.State.Bind(ElState, ((ElNode)e.State[ElState]!).Delete(path)),
                predecessor);
        }

        public static bool ElLookup(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor,
            Step.Continuation k)
        {
            return ((ElNode)e.State[ElState]!).Read(args, 0, e, u => k(o,u,e.State,predecessor));
        }

        public static bool ElDump(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor,
            Step.Continuation k)
        {
            ArgumentCountException.Check(nameof(ElDump), 0, args);
            var s = e.State;
            return k(o.Append(((ElNode)s[ElState]!).SortedContents.Select(c => c+"\n").ToArray()),e.Unifications,s,predecessor);
        }

        protected bool Read(object?[] path, int position, BindingEnvironment e, Predicate<BindingList?> k)
        {
            var node = this;
            for (; node != null && position < path.Length; position += 2)
            {
                var key = path[position];
                if (Term.IsGround(key))
                {
                    if (!node.Lookup(key, ref node))
                        return false;
                }
                else
                    return Bind(path, position, e, k);
            }

            return position >= path.Length && k(e.Unifications);
        }

        protected abstract bool Lookup(object? atom, ref ElNode? child);

        protected abstract bool Bind(object?[] path, int position, BindingEnvironment e, Predicate<BindingList?> k);

        public ElNode? Write(ElNode? child, object?[] path, int position) 
            => (child == null) ? Build(path, position) : child.Write(path, position);

        public ElNode Write(params object?[] path) => Write(path, 0);

        protected abstract ElNode Write(object?[] path, int position);

        public ElNode? Delete(params object?[] path)
        {
            if (path.Length % 2 != 0)
                throw new ArgumentException("Attempting to delete path with an odd length");
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
            private static readonly ImmutableDictionary<object, ElNode?> EmptyDictionary
                = ImmutableDictionary.Create<object, ElNode?>(Term.Comparer.Default);
            private NonExclusive(ImmutableDictionary<object, ElNode?> children)
            {
                this.children = children;
            }

            public NonExclusive() : this(EmptyDictionary)
            { }

            private readonly ImmutableDictionary<object, ElNode?> children;

            public NonExclusive(object? key, ElNode? child) : this(EmptyDictionary.Add(key,child))
            { }

            protected override ElNode Replace(object? key, ElNode? child) =>
                children.TryGetValue(key, out var oldChild) && child == oldChild
                    ? this
                    : new NonExclusive(children.SetItem(key, child));

            protected override ElNode? ChildOf(object? key)
                => children.TryGetValue(key, out var child) ? child : null;

            protected override ElNode DeleteKey(object? key)
            {
                if (children.ContainsKey(key!))
                    return new NonExclusive(children.Remove(key!));
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
                    foreach (var pair in children)
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

            protected override bool Lookup(object? atom, ref ElNode? child)
            {
                if (!children.TryGetValue(atom!, out var node))
                    return false;
                child = node;
                return true;
            }

            protected override bool Bind(object?[] path, int position, BindingEnvironment e, Predicate<BindingList?> k)
            {
                foreach (var pair in children)
                {
                    var key = pair.Key;
                    var child = pair.Value;
                    if (!e.Unify(path[position], key, out BindingList? u))
                        continue;
                    if (position == path.Length - 2) // end of path
                        return k(u);
                    if (child == null)
                        continue;
                    if (child.Read(path, position + 2, new BindingEnvironment(e, u, e.State), k))
                        return true;
                }

                return false;
            }
        }

        private sealed class Exclusive : ElNode
        {
            // ReSharper disable once MemberCanBePrivate.Local
            public readonly object? Key;
            // ReSharper disable once MemberCanBePrivate.Local
            public readonly ElNode? Child;
            public Exclusive(object? key, ElNode? child)
            {
                Key = key;
                Child = child;
            }

            protected override ElNode Replace(object? key, ElNode? child)
            {
                if (Term.LiterallyEqual(key, Key) && child == Child) return this;
                return new Exclusive(key, child);
            }

            protected override ElNode? ChildOf(object? key)
                => (Key == key) ? Child : null;

            protected override ElNode? DeleteKey(object? key)
            {
                return Term.LiterallyEqual(Key, key) ? null : this;
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
                        if (Term.LiterallyEqual(k, Key)) return this;
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

            protected override bool Lookup(object? atom, ref ElNode? child)
            {
                if (!Term.LiterallyEqual(Key, atom)) return false;
                child = Child;
                return true;
            }

            protected override bool Bind(object?[] path, int position, BindingEnvironment e, Predicate<BindingList?> k)
            {
                if (!e.Unify(path[position], Key, out BindingList? u))
                    return false;
                if (position == path.Length - 2) // end of path
                    k(u);
                else if (Child == null)
                    return false;
                return Child!.Read(path, position + 2, new BindingEnvironment(e, u, e.State), k);
            }
        }

        private sealed class Leaf : ElNode
        {
            // ReSharper disable once MemberCanBePrivate.Local
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
                        return Build(path, position)!;
                }
            }

            public override IEnumerable<string> Contents => new[] { Key.ToTermString() };

            protected override ElNode Replace(object? key, ElNode? child)
            {
                throw new NotImplementedException();
            }

            protected override ElNode? DeleteKey(object? key)
            {
                return (Key == key) ? null : this;
            }

            protected override ElNode ChildOf(object? key)
            {
                throw new NotImplementedException();
            }

            protected override bool Lookup(object? atom, ref ElNode? child)
            {
                if (!Term.LiterallyEqual(Key,atom)) return false;
                child = null;
                return true;
            }

            protected override bool Bind(object?[] path, int position, BindingEnvironment e, Predicate<BindingList?> k)
            {
                return position == path.Length-2 // Can't match a leaf someplace other than at the end of a path
                       && e.Unify(path[position], Key, out BindingList? u)
                       && Read(path, position + 2, new BindingEnvironment(e, u, e.State), k);
            }
        }
    }
}
