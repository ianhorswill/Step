using Step;
using Step.Interpreter;
using Step.Output;

namespace Repl
{
    internal static class StepCode
    {
        public static Exception? LastException;

        public static Module Module = null!;
        public static Module ReplUtilities;
        internal static State State;

        private const string ProjectKey = "Project folder";

        public static string ProjectDirectory
        {
            get => Preferences.Get(ProjectKey, "");
            set => Preferences.Set(ProjectKey, value);
        }

        public static string ProjectName => Path.GetFileName(ProjectDirectory);

        static StepCode()
        {
            ReplUtilities = new Module("ReplUtilities", Module.Global);
            ReplUtilities.AddDefinitions(
                "predicate TestCase ?code.",
                "predicate Button ?label ?code.",
                "RunTestCases: [ForEach [TestCase ?call] [RunTestCase ?call]] All tests passed!",
                "RunTestCase ?call: Running ?call ... [Paragraph] [SampleOutputText] [Call ?call] [SampleOutputText]",
                "Test ?task ?testCount: [CountAttempts ?attempt] Test: ?attempt/Write [Paragraph] [Once ?task] [SampleOutputText] [= ?attempt ?testCount]",
                "Sample ?task ?testCount ?sampling: [EmptyCallSummary ?sampling] [CountAttempts ?attempt] Test: ?attempt [Paragraph] [Once ?task] [NoteCalledTasks ?sampling] [SampleOutputText] [= ?attempt ?testCount]",
                "Debug ?task: [Break \"Press F10 to run one step, F5 to finish execution without stopping.\"] [begin ?task]",
                "CallCounts ?task ?subTaskPredicate ?count: [IgnoreOutput [Sample ?task ?count ?s]] [ForEach [?subTaskPredicate ?t] [Write ?t] [Write \"<pos=400>\"] [DisplayCallCount ?s ?t ?count] [NewLine]]",
                "DisplayCallCount ?s ?t ?count: [?s ?t ?value] [set ?average = ?value/?count] [Write ?average]",
                "Uncalled ?task ?subTaskPredicate ?count: [IgnoreOutput [Sample ?task ?count ?s]] [ForEach [?subTaskPredicate ?t] [Write ?t] [Not [?s ?t ?value]] [Write ?t] [NewLine]]",
                "predicate HotKey ?key ?doc ?implementation.",
                "RunHotKey ?key: [firstOf] [HotKey ?key ? ?code] [else] [= ?code [UndefinedHotKey ?key]] [end] [firstOf] [Call ?code] [else] Command failed: ?code/Write [end]",
                "UndefinedHotKey ?key: ?key/Write is not a defined hot key.",
                "ShowHotKeys: <b>Key <indent=100> Function </indent></b> [NewLine] [ForEach [HotKey ?key ?doc ?] [WriteHotKeyDocs ?key ?doc]]",
                "WriteHotKeyDocs ?k ?d: Alt- ?k/Write <indent=100> ?d/Write </indent> [NewLine]",
                "[main] predicate Button ?label ?code.",
                "FindAllButtons ?buttonList: [FindAll [?label ?code] [Button ?label ?code] ?buttonList]",
                "Link ?.", 
                "EndLink.");
            ReplUtilities["PrintLocalBindings"] = new GeneralPrimitive("PrintLocalBindings",
                (args, o, bindings, p, k) =>
                {
                    ArgumentCountException.Check("PrintLocalBindings", 0, args);
                    var locals = bindings.Frame.Locals;
                    var output = new string[locals.Length * 4];
                    var index = 0;
                    foreach (var v in locals)
                    {
                        output[index++] = v.Name.Name;
                        output[index++] = "=";
                        var value = bindings.CopyTerm(v);
                        output[index++] = Writer.TermToString(value); //+$":{value.GetType().Name}";
                        output[index++] = TextUtilities.NewLineToken;
                    }

                    return k(o.Append(TextUtilities.FreshLineToken).Append(output), bindings.Unifications, bindings.State, p);
                });

            var addButton = "AddButton";
            ReplUtilities[addButton] =
                new GeneralPrimitive(addButton, (args, o, e, d, k) =>
                {
                    ArgumentCountException.Check(addButton, 2, args);
                    var name = args[0].ToTermString();
                    var action = ArgumentTypeException.Cast<object[]>(addButton, args[1], args);
                    if (!e.TryCopyGround(action, out var finalAction))
                        throw new ArgumentInstantiationException(addButton, e, args);
                    MainThread.BeginInvokeOnMainThread(() => ReplPage.Instance.AddButton(name, (object[])finalAction!, e.State));
                    return k(o, e.Unifications, e.State, d);
                });

            ReloadStepCode();
        }

        public static void ReloadStepCode()
        {
            LastException = null;
            if (ReplUtilities["Button"] is CompoundTask button)
            {
                button.Methods.Clear();
                // Make sure it gets executed as a predicate
                button.Declare(CompoundTask.TaskFlags.Fallible | CompoundTask.TaskFlags.MultipleSolutions);
            }

            Module = new Module("Main", ReplUtilities)
            {
                FormattingOptions = { ParagraphMarker = "\n\n", LineSeparator = "\n" }
            };

            State = State.Empty;
            try
            {
                if (ProjectDirectory != "")
                    Module.LoadDirectory(ProjectDirectory);
                ReplPage.Instance.RemoveProjectMenu();
                foreach (object[] spec in Module.CallFunction<object[]>("FindAllButtons"))
                {
                    var label = spec[0] as string[];
                    if (label == null)
                    {
                        if (spec[0] is object[] objectArray)
                            label = objectArray.Cast<string>().ToArray();
                        else
                            throw new ArgumentException($"Label on button is not text: {Writer.TermToString(spec[0])}");
                    }

                    var stringLabel = label.Untokenize();
                    var code = spec[1] as object[];
                    if (code == null)
                        throw new ArgumentException(
                            $"Invalid code {Writer.TermToString(spec[1])} to run for button {stringLabel}");
                    ReplPage.Instance.AddProjectMenuEntry(stringLabel, code);
                }
            }
            catch (Exception e)
            {
                LastException = e;
            }
        }

        public static Task<string> Eval(string command)
        {
            command = command.Trim();
            if (!command.StartsWith("["))
                command = $"[{command}]";
            command = $"[Begin {command} [PrintLocalBindings]]";
            return Eval(new StepThread(Module, command, State));
        }

        public static async Task<string> Eval(StepThread stepThread)
        {
            LastException = null;
            string output;
            State? newState;

            try
            {
#pragma warning disable CS8600
                (output, newState) = await stepThread.Start();
#pragma warning restore CS8600
            }
            catch (Exception e)
            {
                LastException = e;
                output = "";
                newState = State;
            }
            if (newState != null)
                State = newState.Value;
            return output!;
        }
    }
}
