﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Step.Interpreter;

namespace Step.Output
{
    /// <summary>
    /// Formatted writing of Step terms (i.e. tuples, variables, and atomic values)
    /// </summary>
    public static class Writer
    {
        /// <summary>
        /// Convert a Step term to a string as it would appear in the source code.
        /// </summary>
        public static string TermToString(object? term, BindingList? bindings = null)
        {
            var b = new StringBuilder();

            void WriteCompound(char before, IEnumerable objects, char after)
            {
                b.Append(before);
                var firstOne = true;
                foreach (var e in objects)
                {
                    if (firstOne)
                        firstOne = false;
                    else
                        b.Append(' ');
                    Walk(e);
                }

                b.Append(after);
            }

            void Walk(object? o)
            {
                switch (o)
                {
                    case Cons list:
                        if (list == Cons.Empty)
                            b.Append("Empty");
                        else
                            WriteCompound('(', list, ')');
                        break;

                    case object[] tuple:
                        WriteCompound('[', tuple, ']');
                        break;

                    case Pair pair:
                        b.Append('[');
                        object? next = pair;
                        var firstOne = true;
                        while (next is Pair p)
                        {
                            if (!firstOne)
                                b.Append(' ');
                            else 
                                firstOne = false;
                            Walk(BindingEnvironment.Deref(p.First, bindings));
                            next = BindingEnvironment.Deref(p.Rest, bindings);
                        }
                        if (next is IList tail)
                            foreach (var e in tail)
                            {
                                b.Append(' ');
                                Walk(BindingEnvironment.Deref(e, bindings));
                            }
                        else
                        {
                            b.Append(" | ");
                            Walk(next);
                        }
                        b.Append(']');
                        break;

                    case LogicVariable l:
                        var d = BindingEnvironment.Deref(l, bindings);
                        if (d == l)
                            // Unbound variable
                            b.Append(l);
                        else
                            Walk(d);
                        break;

                    case IDictionary dict:
                        WriteDictionary(dict);
                        break;

                    case IList l:
                        WriteList(l);
                        break;

                    default:
                        b.Append(o);
                        break;
                }
            }

            void WriteDictionary(IDictionary d)
            {
                b.Append("{");
                var count = 0;
                foreach (DictionaryEntry pair in d)
                {
                    if (count++ == 20)
                    {
                        b.Append(" ... }");
                        return;
                    }

                    if (count != 1)
                        b.Append(" ");

                    b.Append(pair.Key);
                    b.Append(":");
                    var v = pair.Value;
                    WriteFlat(v);
                }

                b.Append("}");
            }

            void WriteList(IList d)
            {
                b.Append("[");
                var count = 0;
                foreach (var  elt in d)
                {
                    if (count++ == 20)
                    {
                        b.Append(" ... ]");
                        return;
                    }

                    if (count != 1)
                        b.Append(" ");

                    WriteFlat(elt);
                }

                b.Append("]");
            }

            void WriteFlat(object v)
            {
                switch (v)
                {
                    case IDictionary d:
                        b.Append(d.Count == 0 ? "{ }" : "{ ... }");
                        break;

                    case IList l:
                        b.Append(l.Count == 0 ? "[]" : "[ ... ]");
                        break;

                    default:
                        b.Append(v);
                        break;
                }
            }

            Walk(term);
            
            return b.ToString();
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
