using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Step.Interpreter;
using Step.Output;

namespace Step.Utilities
{
    /// <summary>
    /// Utilities for generating documentation about tasks
    /// </summary>
    public static class Documentation
    {
        private static string ManualEntryMaybeRichText(Task t)
        {
            var formatter = Module.RichTextStackTraces ? UnityRichTextFormatter : RawTextFormatter;
            return formatter.FormatDocumentation(t);
        }

        /// <summary>
        /// Add bindings for documentation functions
        /// </summary>
        /// <param name="m"></param>
        public static void DefineGlobals(Module m)
        {
            m["Help"] = new DeterministicTextGenerator<Task>("Help", t => new[]
            {
                ManualEntryMaybeRichText(t)
            })
                .Arguments("task")
                .Documentation("documentation", "Print documentation for task");

            m[nameof(Apropos)] = new GeneralPrimitive(nameof(Apropos), Apropos)
                .Arguments("topic")
                .Documentation("documentation", "Print documentation for all tasks that mention topic.");
        }

        /// <summary>
        /// Find all tasks that contain the specified string in their name, arglists, or documentation, and print their documentation.
        /// </summary>
        private static bool Apropos(object?[] args, TextBuffer o, BindingEnvironment e,
            MethodCallFrame? predecessor, Interpreter.Step.Continuation k)
        {
            ArgumentCountException.Check(nameof(Apropos), 1, args);
            var topic = ArgumentTypeException.Cast<string>(nameof(Apropos), args[0], args);
            var output = new StringBuilder();

            bool Relevant(string? x) => x != null && x.IndexOf(topic, StringComparison.InvariantCultureIgnoreCase) >= 0;

            var module = e.Module;
            foreach (var binding in module.AllBindings)
                if (binding.Value is Task t && (Relevant(binding.Key.Name) || Relevant(t.Description)))
                {
                    output.Append(ManualEntryMaybeRichText(t));
                    output.Append("\n\n");
                }

            return k(o.Append(output.ToString()), e.Unifications, e.State, predecessor);
        }

        /// <summary>
        /// Write a little manual in HTML format
        /// </summary>
        /// <param name="m">Module to get definitions from</param>
        /// <param name="path">Path to write the file to</param>
        // ReSharper disable once UnusedMember.Global
        public static void WriteHtmlReference(Module m, string path)
        {
            var root = new ManualSection();

            foreach (var b in m.AllBindings)
                if (b.Value is Task { HasDocumentation: true } t)
                    root.Add(t, t.ManualSection??"Miscellaneous");

            foreach (var (section, intro) in SectionIntroductions)
                root.SectionPath(section).Introduction = intro;

            using var file = File.CreateText(path);

            void RenderSection(string name, ManualSection s, int level)
            {
                file.WriteLine($"<h{level}>{name.Capitalize()}</h{level}>");
                if (s.Introduction != null)
                {
                    file.WriteLine(s.Introduction);
                    file.WriteLine("<p>");
                }
                foreach (var task in s.Tasks.OrderBy(t => t.Name))
                {
                    file.WriteLine(HtmlFormatter.FormatDocumentation(task));
                    file.WriteLine("<p>");
                }

                foreach (var sub in s.Subsections.OrderBy(pair => pair.Key))
                    RenderSection(sub.Key, sub.Value, level+1);
            }

            RenderSection("Task reference", root, 1);
        }

        private class ManualSection
        {
            public readonly Dictionary<string, ManualSection> Subsections = new Dictionary<string, ManualSection>();
            public readonly List<Task> Tasks = new List<Task>();
            public string? Introduction;

            private ManualSection Subsection(string name)
            {
                if (!Subsections.TryGetValue(name, out var sub))
                    Subsections[name] = sub = new ManualSection();
                return sub;
            }

            private void Add(Task t, string[] sectionPath)
            {
                Subsection(sectionPath).Tasks.Add(t);
            }

            private ManualSection Subsection(string[] sectionPath)
            {
                var sec = this;
                foreach (var sub in sectionPath)
                    sec = sec.Subsection(sub);
                return sec;
            }

            public ManualSection SectionPath(string sectionPath) =>
                Subsection(sectionPath.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries));

            private static readonly string[] PathSeparator = {"//"}; 

            public void Add(Task t, string sectionPath) =>
                Add(t, sectionPath.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries));
        }

        private static readonly List<(string, string)> SectionIntroductions = new List<(string, string)>();

        /// <summary>
        /// Specify text to print in the manual between the heading for a section and the first task description.
        /// </summary>
        public static void SectionIntroduction(string sectionPath, string intro) =>
            SectionIntroductions.Add((sectionPath, intro));

        /// <summary>
        /// Formats documentation for a given output language (txt, unity rich text, or html)
        /// </summary>
        public class DocumentationFormatter
        {
            private readonly Func<string, string> escapeText;
            private readonly string startArgument;
            private readonly string endArgument;
            private readonly string startCode;
            private readonly string endCode;
            private readonly string startTaskName;
            private readonly string endTaskName;
            private readonly string lineBreak;

            /// <summary>
            /// Define the format of a particular output format for printing documentation.
            /// All arguments are optional.
            /// </summary>
            /// <param name="escapeText">Function for escaping special characters in a string</param>
            /// <param name="startArgument">Prefix to print before an argument name</param>
            /// <param name="endArgument">Suffix to print after an argument name</param>
            /// <param name="startCode">Prefix to print before code</param>
            /// <param name="endCode">Suffix to print after code</param>
            /// <param name="startTaskName">Prefix to print before a task name</param>
            /// <param name="endTaskName">Suffix to print after a task name</param>
            /// <param name="lineBreak">Code to use to force a line break</param>
            public DocumentationFormatter(Func<string, string>? escapeText = null,
                string startArgument="",
                string endArgument="",
                string startCode="", 
                string endCode="",
                string startTaskName="",
                string endTaskName="",
                string lineBreak="\n")
            {
                this.escapeText = escapeText??(s=>s);
                this.startArgument = startArgument;
                this.endArgument = endArgument;
                this.startCode = startCode;
                this.endCode = endCode;
                this.startTaskName = startTaskName;
                this.endTaskName = endTaskName;
                this.lineBreak = lineBreak;
            }

            /// <summary>
            /// Format a string for this output format
            /// </summary>
            public string FormatString(string s) => escapeText(s);

            /// <summary>
            /// Format an argument name for this output format
            /// </summary>
            public string FormatArgument(string name) => $"{startArgument}{escapeText(name)}{endArgument}";
            /// <summary>
            /// Format a task name for this output format
            /// </summary>
            public string FormatTaskName(string name) => $"{startTaskName}{escapeText(name)}{endTaskName}";

            /// <summary>
            /// Format a call signature for this output format
            /// </summary>
            public string FormatCall(string task, string[] arguments)
            {
                var args = (arguments.Length == 0)
                    ? ""
                    : arguments.Select(name => $" {FormatArgument(name)}").Aggregate(string.Concat);
                return $"{startCode}[{FormatTaskName(task)}{args}]{endCode}";
            }

            /// <summary>
            /// Generate the documentation for a task in this output format
            /// </summary>
            /// <param name="t">Task to document</param>
            /// <returns>Documentation</returns>
            public string FormatDocumentation(Task t) =>
                $"{FormatCall(t.Name, t.Arglist!)}{lineBreak}{FormatString(t.Description!)}";
        }

        /// <summary>
        /// Formats documentation as raw text with now markup.
        /// </summary>
        public static readonly DocumentationFormatter RawTextFormatter = new DocumentationFormatter();

        /// <summary>
        /// Formats documentation with markup for the Unity's rich text text boxes
        /// </summary>
        public static readonly DocumentationFormatter UnityRichTextFormatter =
            new DocumentationFormatter(startArgument: "<i>", endArgument: "</i>",
                startTaskName: "<b>", endTaskName: "</b>");

        /// <summary>
        /// Formats documentation with HTML markup
        /// </summary>
        public static readonly DocumentationFormatter HtmlFormatter =
            new DocumentationFormatter(
                escapeText: EscapeStringForHtml,
                startArgument: "<i>", endArgument: "</i>",
                startTaskName: "<b>", endTaskName: "</b>",
                startCode: "<code>", endCode: "</code>",
                lineBreak: "<br>"
                //lineBreak: "<dd>"
                );

        private static string EscapeStringForHtml(string input)
        {
            var b = new StringBuilder();
            foreach (var c in input)
                switch (c)
                {
                    case '&':
                        b.Append("&amp;");
                        break;

                    case '<':
                        b.Append("&lt;");
                        break;

                    case '>':
                        b.Append("&gt;");
                        break;

                    case '"':
                        b.Append("&quot;");
                        break;

                    default:
                        b.Append(c);
                        break;
                }

            return b.ToString();
        }
    }
}
