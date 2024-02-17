using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data.Converters;

namespace AvaloniaRepl
{
    public class HtmlTextFormatter : IValueConverter
    {
        public readonly static HtmlTextFormatter Instance = new HtmlTextFormatter();

        public static void SetFormattedText(TextBlock textBlock, string formattedText)
        {
            textBlock.Inlines.Clear();
            textBlock.Inlines.Add(ParseHtml(formattedText));
        }

        private static readonly char[] separators = { '<', '>' };

        /// <summary>
        /// Convert an HTML-ish string into an Avalonia "Span",
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Span ParseHtml(string html)
        {
            var split = Regex.Split(html, @"(\<|\>)").Where(s => s != "").ToArray();
            var tokens = new Queue<string>(split);
            var stack = new Stack<(string terminator, Span data)>();
            var topLevelSpan = new Span();
            stack.Push(("", topLevelSpan));

            void AddToken(string token) => stack.Peek().data.Inlines.Add(token);

            void StartSpan(string start, string end, Span span)
            {
                var closeToken = NextToken();
                if (closeToken != ">")
                {
                    AddToken("<");
                    AddToken(start);
                    AddToken(closeToken);
                }
                else
                {
                    stack.Push((end, span));
                }
            }

            void EndSpan()
            {
                var current = stack.Pop().data;
                if (stack.Count > 0)
                    stack.Peek().data.Inlines.Add(current);
            }

            string NextToken()
            {
                if (tokens.Count == 0)
                    return "";
                return tokens.Dequeue();
            }

            while (tokens.Count > 0)
            {
                var t = NextToken();
                switch (t)
                {
                    case "<":
                        var type = NextToken();
                        switch (type.Trim())
                        {
                            case "b":
                                StartSpan(t, "/b", new Bold());
                                break;

                            case "i":
                                StartSpan(t, "/i", new Italic());
                                break;

                            default:
                                var close = NextToken();
                                if (type.Replace(" ", "") == stack.Peek().terminator && close == ">")
                                {
                                    EndSpan();
                                }
                                else
                                {
                                    AddToken("<");
                                    AddToken(t);
                                    AddToken(close);
                                }

                                break;
                        }
                        break;

                    default:
                        AddToken(t);
                        break;
                }
            }

            while (stack.Count > 0) EndSpan();
            return topLevelSpan;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var str = value as string;
            if (str == null || targetType != typeof(InlineCollection))
                throw new NotImplementedException();
            return new InlineCollection() { ParseHtml(str) };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
