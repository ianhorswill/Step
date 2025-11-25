#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using StepRepl.GraphVisualization;
using StepRepl.Views;
using Step;
using Step.Interpreter;
using Step.Output;
using Step.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace StepRepl
{
    public record StepButton(string Label, object[] Action, State State);
    public static class StepCode
    {
        public static Exception? LastException;

        public static Module Module = null!;
        public static Module ReplUtilities;
        internal static State State;

        private const string ProjectKey = "Project folder";

        public static string ProjectDirectory
        {
            get => Preferences.Get(ProjectKey, "");
            set
            {
                if (value.EndsWith("/") || value.EndsWith("\\"))
                    value = value.Substring(0, value.Length - 1);
                Preferences.Set(ProjectKey, value);
            }
        }

        private static bool projectChanged;

        private static FileSystemWatcher? watcher;

        public static void UpdateWatcher(bool watch)
        {
            watcher?.Dispose();
            if (watch)
            {
                watcher = new FileSystemWatcher(StepCode.ProjectDirectory)
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true
                };
                watcher.Filters.Add("*.step");
                watcher.Filters.Add("*.csv");
                watcher.Changed += OnChange;
                watcher.Created += OnChange;
                watcher.Deleted += OnChange;
                watcher.Renamed += OnChange;
            }
            else
            {
                watcher = null;
                projectChanged = false;
            }
        }

        private static void OnChange(object sender, FileSystemEventArgs e)
        {
            projectChanged = true;
        }

        public static string ProjectName => Path.GetFileName(ProjectDirectory);

        public static bool RetainState = true;
        public static Step.Interpreter.Task? CommandProcessor;

        private static void AddDocumentation(string taskName, string section, string docstring) =>
            (((Step.Interpreter.Task) ReplUtilities[taskName]!)!).Documentation(section, docstring);

        static StepCode()
        {
            Step.EnvironmentOption.Handler += EnvironmentOption;
            StepThread.WrapExceptions = true;

            ReplUtilities = new Module("ReplUtilities", Module.Global)
            {
                ["ClearOutput"] = new GeneralPrimitive("ClearOutput",
                        // ReSharper disable once UnusedParameter.Local
                        (args, o, bindings, p, k) =>
                            k(new TextBuffer(o.Buffer.Length), bindings.Unifications, bindings.State, p))
                    .Arguments()
                    .Documentation("StepRepl//display control", "Throws away any previously generated output"),

                ["SampleOutputText"] = new GeneralPrimitive("SampleOutputText",
                        (args, o, bindings, p, k) =>
                        {
                            ArgumentCountException.Check("SampleOutputText", 0, args, o);
                            var t = StepThread.Current;
                            // Don't generate another sample if the last one hasn't been displayed yet.
                            if (!t!.NewSample)
                            {
                                t.Text = o.AsString;
                                t.State = bindings.State;
                                t.NewSample = true;
                            }

                            return k(o, bindings.Unifications, bindings.State, p);
                        })
                    .Arguments()
                    .Documentation("StepRepl//display control",
                        "Update the screen with a snapshot of the current output, even if the program hasn't finished running yet.  This is used for testing code that is running something over and over again so you can see that it's still running."),

                ["EmptyCallSummary"] = new GeneralPredicate<Dictionary<CompoundTask, int>>("EmptyCallSummary",
                        _ => false,
                        () => new[] { new Dictionary<CompoundTask, int>() }
                    )
                    .Arguments("?summary")
                    .Documentation("StepRepl//profiling",
                        "Makes a call summary object that can be used with NoteCalledTasks to record what tasks have been called."),

                ["NoteCalledTasks"] = new GeneralPrimitive("NoteCalledTasks",
                        (args, output, env, predecessor, k) =>
                        {
                            ArgumentCountException.Check("NoteCalledTasks", 1, args, output);
                            var callSummary =
                                ArgumentTypeException.Cast<Dictionary<CompoundTask, int>>("NoteCalledTasks", args[0],
                                    args, output);
                            foreach (var frame in MethodCallFrame.GoalChain(predecessor))
                            {
                                var task = frame.Method!.Task;
                                callSummary.TryGetValue(task, out var previousCount);
                                callSummary[task] = previousCount + 1;
                            }

                            return k(output, env.Unifications, env.State, predecessor);
                        })
                    .Arguments("call_summary")
                    .Documentation("StepRepl//profiling",
                        "Adds all the tasks that were successfully executed on the path leading to this call to the specified call summary."),

            };

            Documentation.SectionIntroduction("StepRepl",
                "These tasks are defined by the StepRepl IDE.  To use them within a game not running inside StepRepl, you would need to copy their source into your game.");
            Documentation.SectionIntroduction("StepRepl//internals", "These are internal functions used by StepRepl.");
            Documentation.SectionIntroduction("StepRepl//display control",
                "Tasks that control how and when text is displayed on the screen.");
            Documentation.SectionIntroduction("StepRepl//profiling",
                "Tasks used to check how often other tasks are run.");
            Documentation.SectionIntroduction("StepRepl//user interaction",
                "Tasks used to allow user control of Step code.");
            Documentation.UserDefinedSystemTask("MenuItem", "menu_name", "item_name", "call")
                .Documentation(
                    "User-defined.  If defined, specifies additional menus to add to the REPL.  For each solution to [MenuItem ?menu ?item ?call]], it will add an item with the specified name to the specified menu that called the specified code.");

            StepGraph.AddPrimitives(ReplUtilities);
            ReplUtilities.AddDefinitions(
                "predicate Button ?label ?code.",
                "Test ?task ?testCount: [CountAttempts ?attempt] Test: ?attempt/Write [Paragraph] [Once ?task] [SampleOutputText] [= ?attempt ?testCount]",
                "SampleStack ?task ?testCount ?sampling: [EmptyCallSummary ?sampling] [CountAttempts ?attempt] Test: ?attempt [Paragraph] [Once ?task] [NoteCalledTasks ?sampling] [SampleOutputText] [= ?attempt ?testCount]",
                "Debug ?task: [Break \"Press F10 to run one step, F5 to finish execution without stopping.\"] [begin ?task]",
                "CallCounts ?task ?subTaskPredicate ?count: [IgnoreOutput [Sample ?task ?count ?s]] [ForEach [?subTaskPredicate ?t] [Write ?t] [Write \"<pos=400>\"] [DisplayCallCount ?s ?t ?count] [NewLine]]",
                "DisplayCallCount ?s ?t ?count: [?s ?t ?value] [set ?average = ?value/?count] [Write ?average]",
                "Uncalled ?task ?subTaskPredicate ?count: [IgnoreOutput [Sample ?task ?count ?s]] [ForEach [?subTaskPredicate ?t] [Write ?t] [Not [?s ?t ?value]] [Write ?t] [NewLine]]");

            AddDocumentation("Test", "StepRepl//testing", "Runs ?task ?testCount times, showing its output each time");
            AddDocumentation("SampleStack", "StepRepl//profiling",
                "Runs ?task ?testCount times, and returns a sampling of the call stack in ?sampling.");
            AddDocumentation("CallCounts", "StepRepl//profiling",
                "Runs ?Task ?count times, then displays the counts of every subtask that satisfies ?subTaskPredicate.");
            AddDocumentation("Uncalled", "StepRepl//profiling",
                "Runs ?task ?count times, then displays every task satisfying ?subTaskPredicate that is never called.");
            
            ReplUtilities["PrintLocalBindings"] = new GeneralPrimitive("PrintLocalBindings",
                    (args, o, bindings, p, k) =>
                    {
                        ArgumentCountException.Check("PrintLocalBindings", 0, args, o);
                        var locals = bindings.Frame.Locals;
                        var output = new string[locals.Length * 4];
                        var index = 0;
                        foreach (var v in locals)
                        {
                            output[index++] = v.Name.Name;
                            output[index++] = "=";
                            var value = bindings.CopyTerm(v);
                            output[index++] = Writer.TermToString(value, bindings.Unifications); //+$":{value.GetType().Name}";
                            output[index++] = TextUtilities.NewLineToken;
                        }

                        return k(o.Append(TextUtilities.FreshLineToken).Append(output), bindings.Unifications,
                            bindings.State, p);
                    })
                .Arguments()
                .Documentation("StepRepl//internals",
                    "Prints the values of all local variables.  There probably isn't any reason for you to use this directly, but it's used by StepRepl to print the results of queries.");

            var addButton = "AddButton";
            ReplUtilities[addButton] =
                new GeneralPrimitive(addButton, (args, o, e, d, k) =>
                    {
                        ArgumentCountException.Check(addButton, 2, args, o);
                        var name = Writer.HumanForm(args[0], e.Unifications);
                        // Kluge: Avalonia treats the first instance of an _ in a label specially.
                        // So we have to escape all the _'s by doubling them.
                        name = name.Replace("_", "__");

                        if (!e.TryCopyGround(args[1], out var action))
                            throw new ArgumentInstantiationException(addButton, e, args, o);
                        var finalAction = ArgumentTypeException.Cast<object[]>(addButton, action, args, o);
                        
                        StepButton button = new(name, finalAction, e.State);
                        Dispatcher.UIThread.Post(() =>
                        {
                            var activeTabContent = MainWindow.Instance.GetActiveTabContent();
                            if (activeTabContent is TabInfo { Content: RunnerPage runnerPage })
                                runnerPage.RegisterNewButton(button);
                        });

                        return k(o, e.Unifications, e.State, d);
                    })
                    .Arguments("label", "code")
                    .Documentation(
                        "Adds a button with the specified label text that when pressed will run the specified code in the current dynamic state.");
            Autograder.AddBuiltins();
            Importers.SExpressionReader.AddBuiltins(ReplUtilities);
        }

        private static void EnvironmentOption(string option, object?[] args)
        {
            switch (option)
            {
                case "retainState":
                    RetainState = true;
                    break;

                case "discardState":
                    RetainState = false;
                    State = State.Empty;
                    break;

                case "commandProcessor":
                    CommandProcessor = ((Step.Interpreter.Task)args[0]!)!;
                    break;
            }
        }

        public static void ReloadIfNecessary()
        {
            if (projectChanged)
                ReloadStepCode();
        }

        public static void ReloadStepCode()
        {
            projectChanged = false;
            RunnerPage.Singleton.RemoveUserManus();

            Module.DefaultSearchLimit = int.MaxValue;
            LastException = null;
            RetainState = true;
            CommandProcessor = null;
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

                if (ProjectDirectory != "")
                {
                    Console.WriteLine($"Loaded Step project at {ProjectDirectory}");
                    Dispatcher.UIThread.Post(() =>
                    {
                        MainWindow.Instance.SetTabDisplayName(RunnerPage.Singleton, $"{StepCode.ProjectName}");
                        RunnerPage.Singleton.Clear();
                    });
                }

                if (Module.Defines("MenuItem") && Module["MenuItem"] is CompoundTask task)
                {
                    task.Flags |= CompoundTask.TaskFlags.MultipleSolutions | CompoundTask.TaskFlags.Fallible;
                    var menuVar = new LogicVariable("?menu", 0);
                    var itemVar = new LogicVariable("?item", 1);
                    var callVar = new LogicVariable("?call", 2);
                    var menus = new List<(string menu, string item, object?[] call)>();
                    var env = new BindingEnvironment(Module, null!, null, State.Empty);
                    task.Call(new object[] { menuVar, itemVar, callVar },
                        new TextBuffer(), env, null,
                        (o, u, s, f) =>
                        {
                            var menuName = BindingEnvironment.Deref(menuVar, u);
                            var itemName = BindingEnvironment.Deref(itemVar, u);
                            var call = new BindingEnvironment(env, u, s).CopyTerm(callVar) as object?[];
                            if (menuName != null && itemName != null && call != null)
                                menus.Add((Writer.HumanForm(menuName, u), Writer.HumanForm(itemName, u), call));
                            return false;
                        });
                    Dispatcher.UIThread.Post(() =>
                    {
                        foreach (var m in menus)
                            RunnerPage.Singleton.AddMenuItem(m.menu, m.item, m.call);
                    });
                }
            }
            catch (Exception e)
            {
                LastException = e;
                //Console.WriteLine($"Error loading project at {ProjectDirectory}: {e.Message}");
            }

        }

        public static StepThread? CurrentStepThread;

        public static bool StepThreadRunning => CurrentStepThread != null && !CurrentStepThread.IsCompleted;

        public static Task<string> Eval(string command)
        {
            CurrentStepThread = new StepThread(Module, NormalizeCommand(command), State);
            
            return Eval(CurrentStepThread);
        }

        public static Task<string> Eval(object?[] call)
        {
            CurrentStepThread = new StepThread(Module, State, (Step.Interpreter.Task)call[0]!, call.Skip(1).ToArray());
            
            return Eval(CurrentStepThread);
        }

        private static string NormalizeCommand(string command)
        {
            command = command.Trim();
            if (CommandProcessor != null && !command.StartsWith("["))
                command = $"[{CommandProcessor.Name} \"{command}\"]";
            if (!command.StartsWith("["))
                command = $"[{command}]";
            command = $"[Begin {command} [PrintLocalBindings]]";
            return command;
        }

        public static Task<string> EvalWithDebugger(string command, Action<ReplDebugger> debuggerCallback, bool singleStep = true)
        {
            CurrentStepThread = new StepThread(Module, NormalizeCommand(command), State);
            ReplDebugger debugger = new ReplDebugger(CurrentStepThread.Debugger);
            debugger.OnDebugPauseCallback = debuggerCallback;
            debugger.SingleStepping = singleStep;

            return Eval(CurrentStepThread);
        }

        public static void AbortCurrentStepThread()
        {
            if (StepThread.Current != null)
                StepThread.Current.Abort();
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
            catch (StepException w) when (w.InnerException is CallFailedException f && IsTopLevelCallFailure(f))
            {
                return "<b>No</b> (<i>top-level call failed</i>)";
            }
            catch (Exception e) 
            {
                LastException = e is StepException?e.InnerException:e;
                if (LastException is StepExecutionException { SuppressStackTrace: true })
                    LastException = null;
                output = stepThread.Text??"";
                newState = State;
            }
            if (newState != null && RetainState)
                State = newState.Value;
            return output!;
        }

        private static bool IsTopLevelCallFailure(CallFailedException e) => e.Task is CompoundTask { Name: "TopLevelCall" };
    }
}
