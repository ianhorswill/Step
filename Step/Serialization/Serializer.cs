using System;
using System.Collections;
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

                case ISerializable iSerializable:
                    var (start, typeToken, end, includeSpace) = iSerializable.SerializationBracketing();
                    Write(start);
                    Write(typeToken);
                    if (includeSpace)
                        Write(' ');
                    iSerializable.Serialize(this);
                    Write(end);
                    break;

                case object[] tuple:
                    Write('[');
                    bool firstOne = true;
                    foreach (var elt in tuple)
                    {
                        if (firstOne)
                            firstOne = false;
                        else 
                            Write(' ');
                        Serialize(elt);
                    }
                    Write(']');
                    break;

                default:
                    throw new ArgumentException($"Cannot serialize the object {o}");
            }
        }

        /// <summary>
        /// Writes a dictionary out in the standard JSON { key = value ... } format
        /// </summary>
        public void SerializeDictionary(IDictionary d, Action<object?> keySerializer, Action<object?> valueSerializer)
        {
            Write('{');
            foreach (DictionaryEntry e in d)
            {
                keySerializer(e.Key);
                Write(" = ");
                valueSerializer(e.Value);
            }
            Write('}');
        }

        public void Space()
        {
            Write(' ');
        }
    }
}
