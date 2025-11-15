using Step.Utilities;
using System;
using System.Xml.Linq;

namespace Step.Interpreter
{
    internal static class MetaTasks
    {
        internal static void DefineGlobals()
        {
            var g = Module.Global;

            Documentation.SectionIntroduction("control flow/metatasks",
                "Tasks used to manage the execution of other tasks.  These are used by tagging the task to be managed with the annotation: [meta MetaTaskName]");

            g[nameof(Symmetric)] = new GeneralPrimitive(nameof(Symmetric), Symmetric)
                .Arguments("call")
                .Documentation("control flow//metatasks", "Used as a metatask for a two-argument predicate to make it symmetric, i.e. the order of its arguments doesn't matter..");
        }

        private static (Task task, object?[] args) CheckBinaryRelation(string name, object?[] args, TextBuffer output, BindingEnvironment env)
        {
            ArgumentCountException.Check(name, 1, args, output);
            var call = ArgumentTypeException.Cast<object?[]>(args, args[0], args, output);
            if (call.Length == 0)
                throw new ArgumentTypeException(name,
                    "Argument should be a call with two arguments, but was the empty list", call, output);
            var task = ArgumentTypeException.Cast<Task>(name, call[0], call, output);
            var tArgs = new object?[call.Length - 1];
            Array.Copy(call, 1, tArgs, 0, tArgs.Length);
            if (task.ArgumentCount.HasValue)
                ArgumentCountException.Check(task, task.ArgumentCount.Value, tArgs, output);
            if (tArgs.Length != 2)
                throw new CallException(name, call, $"Tasks with {name} as a meta-task must have 2 arguments", output);
            tArgs[0] = env.Resolve(tArgs[0]);
            tArgs[1] = env.Resolve(tArgs[1]);
            return (task, tArgs);
        }

        private static bool Symmetric(object?[] args, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? predecessor, Step.Continuation k)
        {
            var (task, tArgs) = CheckBinaryRelation(nameof(Symmetric), args, output, env);

            return task.CallDirect(tArgs, output, env, predecessor, k)
                   || task.CallDirect([tArgs[1], tArgs[0]], output, env, predecessor, k);
        }
    }
}
