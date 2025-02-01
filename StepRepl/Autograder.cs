using Step.Interpreter;
using System;
using System.IO;
using Step;
using Step.Interpreter;
using Step.Utilities;

namespace StepRepl
{
    internal static class Autograder
    {
        public static void AddBuiltins()
        {
            var g = StepCode.ReplUtilities;

            Documentation.SectionIntroduction("StepRepl//utilities",
                "Miscellaneous utility functions allowing manipulation of interpreter data structures.  These are intended for writing auto-graders for classes, which need to load and run student-run programs in protected environments.");

            g["EraseMethods"] = new SimplePredicate<CompoundTask>("EraseMethods", m =>
            {
                m.EraseMethods();
                return true;
            })
            .Arguments("task")
            .Documentation("StepRepl//utilities", "Removes all methods from the task.");

            g["ResetFluent"] = new GeneralPrimitive("ResetFluent", (args, output, env, frame, k) =>
                {
                    ArgumentCountException.Check("ResetFluent", 1, args, output);
                    var fluent = ArgumentTypeException.Cast<CompoundTask>("ResetFluent", args[0], args, output);
                    return k(output, env.Unifications, fluent.ClearCache(env.State), frame);
                })
                .Arguments("task")
                .Documentation("StepRepl//utilities", "Removes all methods from the task.");

        g["MakeModule"] = new SimpleFunction<string, Module>("MakeModule",
            path =>
            {
                var m = new Module(Path.GetFileName(path), g);
                m.LoadDirectory(path);
                return m;
            })
            .Arguments("path", "?module")
            .Documentation("StepRepl//utilities", "Creates a new module (namespace object) and loads the code in the directory path into it.");

        g["LoadDirectory"] = new SimplePredicate<string, Module>("LoadDirectory",
            (path, m) =>
            {
                m.LoadDirectory(path);
                return true;
            })
            .Arguments("path", "module")
            .Documentation("StepRepl//utilities", "Loads the code at directory path into module");

        g["LoadFile"]= new SimplePredicate<string, Module>("LoadFile",
            (path, m) =>
            {
                m.LoadDefinitions(path);
                return true;
            })
            .Arguments("path", "module")
            .Documentation("StepRepl//utilities", "Loads the file at directory path into module");

        g["PathFileName"] = new SimpleFunction<string, string>("PathFileName", Path.GetFileName)
            .Arguments("path", "?name")
            .Documentation("StepRepl//utilities", "True when the filename of path is ?name");
        g["DirectoryFilePath"] = new SimpleFunction<string, string, string>("DirectoryFilePath", Path.Combine)
            .Arguments("directoryPath", "filename", "?combined")
            .Documentation("StepRepl//utilities", "True when ?combined is a path to the file filename in the directory directoryPath.");

        g["DirectoryFile"] = new GeneralPredicate<string, string>("DirectoryFile",
            (d,f) => Path.GetDirectoryName(f) == d,
            Directory.GetFiles, 
            f=> new [] { Path.GetDirectoryName(f) },
            null)
            .Arguments("directoryPath", "filePath")
            .Documentation("StepRepl//utilities", "True when the specified file is in the specified directory.");

        g["DirectorySubdirectory"] = new GeneralPredicate<string, string>("DirectorySubdirectory",
            (d, f) => Path.GetDirectoryName(f) == d,
            Directory.GetDirectories,
            f => new[] { Path.GetDirectoryName(f) },
            null)
            .Arguments("directoryPath", "subPath")
            .Documentation("StepRepl//utilities", "True when the specified subdirectory is in the specified directory.");

        //g["ProjectDirectory"] = new SimpleFunction<string, string>("SubdirectoryHere", Repl.FindProject);

        g[nameof(CallInModule)] = new GeneralPrimitive(nameof(CallInModule), CallInModule)
            .Arguments("call", "module")
            .Documentation("StepRepl//utilities", "Executes call using the tasks and variables inside Module");

        g[nameof(CallResult)] = new GeneralPrimitive(nameof(CallResult), CallResult)
            .Arguments("call", "?result")
            .Documentation("StepRepl//utilities",
                "True when the result of executing call (a tuple) is ?result.  Result is true when the call succeeds, false when it fails, and the exception message when it throw an exception.");

        g["LookupGlobal"] = new SimpleFunction<string[], Module, object>("LookupGlobal", (name, module) => module[name[0]])
            .Arguments("\"name\"", "module", "?value")
            .Documentation("StepRepl//utilities", "True when ?value is the value of the global variable within module whose name is \"name\"");

        g["ParseSubmissionName"] = new GeneralPrimitive("ParseSubmissionName",
            (args, output, env, frame, k ) =>
            {
                ArgumentCountException.Check("ParseSubmissionName", 3, args, output);
                var path = ArgumentTypeException.Cast<string>("ParseSubmissionName", args[0], args, output);
                var fileName = Path.GetFileNameWithoutExtension(path);
                var elements = fileName.Split('_');
                if (elements.Length < 2)
                    throw new ArgumentException($"Invalid file name format: {fileName}");
                var student = elements[0];
                var id = elements[1];
                if (id == "LATE")
                    id = elements[2];
                return env.UnifyArrays(args, new[] {new object[] {path, student, id}}, out BindingList? bindings)
                    && k(output, bindings, env.State, frame);
            })
            .Arguments("path", "?student", "?id")
            .Documentation("StepRepl//utilities", "Parses the filenames generated by the Canvas learning management system.  When path is a path to a file for a student submission downloaded from Canvas, this is true when ?student is the name of the student submitting and ?id is their student ID number.");
    }

    private static bool CallInModule(object[] args, TextBuffer output, BindingEnvironment env,
        MethodCallFrame predecessor,
        Step.Interpreter.Step.Continuation k)
    {
        ArgumentCountException.CheckAtLeast(nameof(CallInModule), 2, args, output);
        var call = ArgumentTypeException.Cast<object[]>(nameof(CallInModule), args[0], args, output);
        var module = ArgumentTypeException.Cast<Module>(nameof(CallInModule), args[1], args, output);

        Task task;
        switch (call[0])
        {
            case Task t:
                task = t;
                break;

            case string s:
                task = module[s] as Task;
                if (task == null)
                    throw new InvalidOperationException(
                        $"Task argument to CallInModule must be a task or name of a task.  It was instead the name {s}, whose value in module {module} is {module[s]}.");
                break;

            case string[] text:
                task = module[text[0]] as Task;
                if (task == null)
                    throw new InvalidOperationException(
                        $"Task argument to CallInModule must be a task or name of a task.  It was instead the name {text[0]}, whose value in module {module} is {module[text[0]]}.");
                break;

            default:
                throw new InvalidOperationException(
                    $"Task argument to CallInModule must be a task or name of a task.  It was instead {call[0]}.");
        }

        
        var taskArgs = new object[call.Length - 1 + args.Length - 2];

        var i = 0;
        for (var callIndex = 2; callIndex < call.Length; callIndex++)
            taskArgs[i++] = call[callIndex];
        for (var argsIndex = 2; argsIndex < args.Length; argsIndex++)
            taskArgs[i++] = args[argsIndex];

        return task.Call(taskArgs, output, new BindingEnvironment(module, env.Frame, env.Unifications, env.State), predecessor, k);
    }

    private static bool CallResult(object[] args, TextBuffer output, BindingEnvironment env,
        MethodCallFrame predecessor, Step.Interpreter.Step.Continuation k)
    {
        ArgumentCountException.Check(nameof(CallResult), 2, args, output);
        var call = ArgumentTypeException.Cast<object[]>(nameof(CallResult), args[0], args, output);

        // Kluge
        var inverted = call.Length == 2 && call[0] == Module.Global["Not"];
        if (inverted)
            call = call[1] as object[];

        // ReSharper disable once PossibleNullReferenceException
        var task = call[0] as Task;
        if (task == null)
            throw new InvalidOperationException(
                $"Task argument to CallResult must be a task.  It was instead {call[0]}.");

        var taskArgs = new object[call.Length - 1];

        var i = 0;
        for (var callIndex = 1; callIndex < call.Length; callIndex++)
            taskArgs[i++] = call[callIndex];

        object result;
        var newOutput = output;
        var unifications= env.Unifications;
        var state = env.State;
        // ReSharper disable once IdentifierTypo
        var pred = predecessor;

        try
        {
            result = inverted ^ task.Call(taskArgs, output, env, predecessor, (o, u, s, p) =>
            {
                newOutput = o;
                unifications = u;
                state = s;
                pred = p;
                return true;
            });
        }
        catch (Exception e)
        {
            result = e.Message;
        }
        
        return env.Unify(args[1], result, unifications, out var finalUnifications)
                && k(newOutput, finalUnifications, state, pred);
    }
    }
}
