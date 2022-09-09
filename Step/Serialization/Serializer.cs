using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Step.Serialization
{
    public class Serializer
    {
        public readonly TextWriter Writer;

        public void Write(char s) => Writer.Write(s);
        public void Write(string s) => Writer.Write(s);

        public void Write(int i) => Writer.Write(i);

        public Serializer(TextWriter writer)
        {
            this.Writer = writer;
        }

        public static string SerializeToString(object o)
        {
            using var str = new StringWriter();
            var s = new Serializer(str);
            s.Serialize(o);
            return str.ToString();
        }

        public void Serialize(object? o)
        {
            switch (o)
            {
                case null:
                    Write("null");
                    break;

                case true:
                    Write("true");
                    break;

                case false:
                    Write("false");
                    break;

                case ISerializable iSerializable:
                    var (start, end, includeSpace) = iSerializable.SerializationBracketing();
                    Write(start);
                    Write(o.GetType().Name);
                    if (includeSpace)
                        Write(' ');
                    iSerializable.Serialize(this);
                    Write(end);
                    break;

                case string str:
                    Write('"');
                    foreach (var c in str)
                        switch (c)
                        {
                            case '"':
                                Write("\\\"");
                                break;

                            case '\\':
                                Write("\\\\");
                                break;

                            default:
                                Write(c);
                                break;
                        }
                    Write('"');
                    break;

                case int i:
                    Write(i);
                    break;

                case float f:
                    var fs = f.ToString("G9");
                    Write(fs);
                    if (!fs.Contains('.'))
                        Write(".0");

                    break;

                default:
                    throw new ArgumentException($"Cannot serialize the object {o}");
            }
        }
    }
}
