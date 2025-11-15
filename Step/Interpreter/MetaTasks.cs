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
            g[nameof(PartialOrder)] = new GeneralPrimitive(nameof(PartialOrder), PartialOrder)
                .Arguments("call")
                .Documentation("control flow//metatasks",
                    "Used as a metatask for a two-argument predicate to make it a partial order.  Predicate must be anti-reflexive.");
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

        private static LocalVariableName Temp = new LocalVariableName("?temp", -1);
        private static bool PartialOrder(object?[] args, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? p, Step.Continuation k)
        {
            var (task, tArgs) = CheckBinaryRelation(nameof(PartialOrder), args, output, env);

            bool Recur(object? a, object? b, TextBuffer output, BindingEnvironment e)
            {
                // Reflexive case
                if (env.Unify(a, b, e.Unifications, out var bindings)
                    && k(output, bindings, env.State, p))
                    return true;
                // Base case
                if (task.CallDirect([a, b], output, e, p, k))
                    return true;
                // Recursive case
                var c = new LogicVariable(Temp);
                return task.CallDirect([a, c], output, e, p,
                    (o, bindings2, s, _) => Recur(c, b, o, new BindingEnvironment(e, bindings2, s)));
            }

            return Recur(tArgs[0], tArgs[1], output, env);
        }
    }
}
