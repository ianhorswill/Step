using System;
using System.Collections;
using System.Text;
using Step.Interpreter;

namespace Step.Utilities
{
    /// <summary>
    /// Formatted writting of Step terms (i.e. tuples, variables, and atomic values)
    /// </summary>
    public static class Writer
    {
        /// <summary>
        /// Convert a Step term to a string as it would appear in the source code.
        /// </summary>
        public static string TermToString(object term, BindingList<LogicVariable> bindings = null)
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

            void Walk(object o)
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

                    case LogicVariable l:
                        var d = BindingEnvironment.Deref(l, bindings);
                        if (d == l)
                            // Unbound variable
                            b.Append(l);
                        else
                            Walk(d);
                        break;

                    case Delegate prim:
                        b.Append(PrimitiveTask.PrimitiveName(prim));
                        break;

                    default:
                        b.Append(o);
                        break;
                }
            }
            
            Walk(term);
            
            return b.ToString();
        }
    }
}
