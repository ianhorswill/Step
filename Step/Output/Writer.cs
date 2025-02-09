using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Step.Interpreter;

namespace Step.Output
{
    /// <summary>
    /// Formatted writing of Step terms (i.e. tuples, variables, and atomic values)
    /// </summary>
    public class Writer
    {
        public readonly StringBuilder Buffer;
        public readonly BindingList? Bindings;

        public Writer(BindingList? bindings, StringBuilder buffer)
        {
            Bindings = bindings;
            Buffer = buffer;
        }
        public Writer(BindingList? bindings) : this(bindings, new StringBuilder())
        { }

        void WriteCompound(char before, IEnumerable objects, char after)
        {
            Buffer.Append(before);
            var firstOne = true;
            foreach (var e in objects)
            {
                if (firstOne)
                    firstOne = false;
                else
                    Buffer.Append(' ');
                Write(e);
            }

            Buffer.Append(after);
        }

        public void Write(object? o)
        {
            switch (o)
            {
                case Cons list:
                    if (list == Cons.Empty)
                        Buffer.Append("Empty");
                    else
                        WriteCompound('(', list, ')');
                    break;

                case object[] tuple:
                    WriteCompound('[', tuple, ']');
                    break;

                case Pair pair:
                    Buffer.Append('[');
                    object? next = pair;
                    var firstOne = true;
                    while (next is Pair p)
                    {
                        if (!firstOne)
                            Buffer.Append(' ');
                        else
                            firstOne = false;
                        Write(BindingEnvironment.Deref(p.First, Bindings));
                        next = BindingEnvironment.Deref(p.Rest, Bindings);
                    }

                    if (next is IList tail)
                        foreach (var e in tail)
                        {
                            Buffer.Append(' ');
                            Write(BindingEnvironment.Deref(e, Bindings));
                        }
                    else
                    {
                        Buffer.Append(" | ");
                        Write(next);
                    }

                    Buffer.Append(']');
                    break;

                case LogicVariable l:
                    var d = BindingEnvironment.Deref(l, Bindings);
                    if (d == l)
                        // Unbound variable
                        Buffer.Append(l);
                    else
                        Write(d);
                    break;

                case LocalVariableName ln:
                    //Buffer.Append("LocalVariableName{");
                    Buffer.Append(ln.Name);
                    //Buffer.Append(':');
                    //Buffer.Append(ln.Index);
                    //Buffer.Append('}');
                    break;

                case FeatureStructure s:
                    s.Write(this);
                    break;

                case IDictionary dict:
                    WriteDictionary(dict);
                    break;

                case IList l:
                    WriteList(l);
                    break;

                case string s:
                    if (s.StartsWith("?") || s.Contains(' '))
                    {
                        Buffer.Append('|');
                        Buffer.Append(s);
                        Buffer.Append('|');
                    }
                    else
                        Buffer.Append(s);

                    break;

                default:
                    Buffer.Append(o);
                    break;
            }
        }

        void WriteDictionary(IDictionary d)
        {
            Buffer.Append("{");
            var count = 0;
            foreach (DictionaryEntry pair in d)
            {
                if (count++ == 20)
                {
                    Buffer.Append(" ... }");
                    return;
                }

                if (count != 1)
                    Buffer.Append(" ");

                Buffer.Append(pair.Key);
                Buffer.Append(":");
                var v = pair.Value;
                WriteFlat(v);
            }

            Buffer.Append("}");
        }

        void WriteList(IList d)
        {
            Buffer.Append("[");
            var count = 0;
            foreach (var elt in d)
            {
                if (count++ == 20)
                {
                    Buffer.Append(" ... ]");
                    return;
                }

                if (count != 1)
                    Buffer.Append(" ");

                WriteFlat(elt);
            }

            Buffer.Append("]");
        }

        void WriteFlat(object v)
        {
            switch (v)
            {
                case IDictionary d:
                    Buffer.Append(d.Count == 0 ? "{ }" : "{ ... }");
                    break;

                case IList l:
                    Buffer.Append(l.Count == 0 ? "[]" : "[ ... ]");
                    break;

                default:
                    Buffer.Append(v);
                    break;
            }
        }

        public override string ToString() => Buffer.ToString();

        /// <summary>
        /// Convert a Step term to a string as it would appear in the source code.
        /// </summary>
        public static string TermToString(object? term, BindingList? bindings = null)
        {
            var w = new Writer(bindings);
            w.Write(term);

            return w.ToString();
        }

        public static string HumanForm(object? o, BindingList? bindings = null) =>
            o switch
            {
                string s => s,
                string[] text => text.Untokenize(),
                _ => TermToString(o, bindings)
            };

        public static string HumanForm(IEnumerable<object?> items, BindingList? bindings = null) =>
            items.SelectMany(o => o switch
            {
                string s => new[] { s },
                string[] text => text,
                _ => new[] { TermToString(o, bindings) }
            }).Untokenize();
    }
}
