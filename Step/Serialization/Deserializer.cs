using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Step.Serialization
{
    public class Deserializer
    {
        public static TextReader Reader;

        public int Peek() => Reader.Peek();
        public int Read() => Reader.Read();

        public Deserializer(TextReader reader)
        {
            Reader = reader;
        }

        public static object Deserialize(TextReader r) => new Deserializer(r).Deserialize();

        public T Expect<T>()
        {
            var o = Deserialize();
            if (o is T t)
                return t;
            throw new InvalidDataException($"Expected an object of type {typeof(T).Name} but got {o}");
        }

        public object Deserialize()
        {
            var firstChar = Peek();
            switch (firstChar)
            {
                case -1:
                    throw new InvalidDataException("Deserialization stream ended prematurely");

                case '"':
                    return ReadString();

                case '-':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case '.':
                    var t = ReadToken("+-0123456789.e");
                    if (t.Contains('.'))
                        return float.Parse(t);
                    return int.Parse(t);

                case '<':
                    case '{':
                        case '[':
                    Read(); // Swallow bracket
                    var type = ReadAlphabetic();
                    SkipIfSpace();
                    if (!HandlerTable.TryGetValue(type, out var handler))
                        throw new InvalidDataException($"Unknown type name {type} in serialization stream");
                    var o = handler(this);
                    var c = (char)Read();
                    var expected = '>';
                    if (firstChar == '{')
                        expected = '}';
                    else if (firstChar == '[')
                        expected = ']';
                    if (c != expected)
                        throw new InvalidDataException(
                            $"Serialized data for type {type} started with {(char)firstChar} but ended with {c}");
                    return o;

                default:
                    throw new InvalidDataException($"Unknown character starting serialized data: {(char)Peek()}");
            }
        }

        public void SkipIfSpace()
        {
            if (Peek() == ' ')
                Read();
        }

        public void SkipWhitespace()
        {
            while (Peek() == ' ')
                Read();
        }

        private readonly StringBuilder stringBuffer = new StringBuilder();

        public string ReadString()
        {
            stringBuffer.Clear();
            Debug.Assert(Peek() == '"');
            Read();
            while (Peek() != '"')
            {
                var c = (char)Read();
                if (c == '\\')
                    c = (char)Read();

                stringBuffer.Append(c);
            }

            Read();
            return stringBuffer.ToString();
        }

        public string ReadToken(string tokenChars)
        {
            stringBuffer.Clear();
            while (tokenChars.Contains((char)Peek()))
                stringBuffer.Append((char)Read());
            return stringBuffer.ToString();
        }

        public string ReadToken(Predicate<char> tokenChar)
        {
            stringBuffer.Clear();
            while (tokenChar((char)Peek()))
                stringBuffer.Append((char)Read());
            return stringBuffer.ToString();
        }

        public string ReadAlphabetic() => ReadToken(char.IsLetter);

        public delegate object DeserializationHandler(Deserializer d);

        private static Dictionary<string, DeserializationHandler> HandlerTable =
            new Dictionary<string, DeserializationHandler>();

        public static void RegisterHandler(string t, DeserializationHandler h) => HandlerTable[t] = h;

        public static void RegisterHandler(Type t, DeserializationHandler h) => RegisterHandler(t.Name, h);
    }
}
