using System.Collections.Generic;
using System.Linq;
using static Step.Interpreter.PrimitiveTask;

namespace Step.Interpreter
{
    static class ReflectionBuiltins
    {
        public static void DefineGlobals()
        {
            var g = Module.Global;

            // Argument is a compound task
            g["CompoundTask"] = NamePrimitive("CompoundTask", (MetaTask) CompoundTask);

            // Second arg is a method of the first arg
            g["TaskMethod"] =
                NamePrimitive("TaskMethod", 
                    GeneralRelation<CompoundTask, Method>(
                    "TaskMethod",
                    (t, m) => m.Task == t,
                    t => t.Methods,
                    m => new[] {m.Task},
                    null));

            // Gets the MethodCallFrame of the most recent call
            g["LastMethodCallFrame"] = NamePrimitive("LastMethodCallFrame", 
                GeneralRelation("LastMethodCallFrame",
                f => f == MethodCallFrame.CurrentFrame,
                () => new[] {MethodCallFrame.CurrentFrame}));

            // Second argument is in the caller chain leading to the first argument
            g["CallerChainAncestor"] = NamePrimitive("CallerChainAncestor",
                GeneralRelation<MethodCallFrame, Method>(
                "CallerChainAncestor",
                // Is this method in this chain?
                (f, m) => f.CallerChain.FirstOrDefault(a => a.Method == m) != null,
                // What methods are in this chain?
                f => f.CallerChain.Select(a => a.Method),
                null,
                null));

            // Second argument is in the goal chain leading to the first argument
            g["GoalChainAncestor"] = NamePrimitive("GoalChainAncestor",
                GeneralRelation<MethodCallFrame, Method>(
                "GoalChainAncestor",
                // Is this method in this chain?
                (f, m) => f.GoalChain.FirstOrDefault(a => a.Method == m) != null,
                // What methods are in this chain?
                f => f.GoalChain.Select(a => a.Method),
                null,
                null));

            // First argument calls the second argument
            g["TaskCalls"] = NamePrimitive("TaskCalls", (MetaTask) TaskCalls);

            // Second argument is a call expression for a call in some method of first argument.
            g["TaskSubtask"] = NamePrimitive("TaskSubtask", (MetaTask) TaskSubtask);
        }

        private static bool CompoundTask(object[] args, PartialOutput o, BindingEnvironment e, Step.Continuation k, MethodCallFrame predecessor)
        {
            ArgumentCountException.Check("CompoundTask", 1, args);
            var arg = e.Resolve(args[0]);
            var l = arg as LogicVariable;
            if (l == null)
                // Argument is instantiated; test if it's a compound task
                return (arg is CompoundTask) && k(o, e.Unifications, e.State, predecessor);
            foreach (var t in e.Module.DefinedTasks)
                if (k(o, BindingList<LogicVariable>.Bind(e.Unifications, l, t), e.State, predecessor))
                    return true;

            return false;
        }

        private static bool TaskCalls(object[] args, PartialOutput o, BindingEnvironment e, Step.Continuation k,
            MethodCallFrame predecessor)
        {
            var m = e.Module;
            ArgumentCountException.Check(nameof(TaskCalls), 2, args);
            ArgumentTypeException.Check(nameof(TaskCalls), typeof(CompoundTask), args[0], args, true);
            var callerVar = args[0] as LogicVariable;
            var callerTask = args[0] as CompoundTask;
            var callee = args[1];
            var calleeVar = callee as LogicVariable;
            if (callerVar == null)
            {
                // First arg is input
                if (calleeVar == null)
                {
                    // in in
                    if (m.TaskCalls(callerTask, callee))
                        return k(o, e.Unifications, e.State, predecessor);
                }
                else
                {
                    // in out
                    foreach (var c in m.Callees(callerTask))
                        if (k(o, BindingList<LogicVariable>.Bind(e.Unifications, calleeVar, c), e.State, predecessor))
                            return true;
                }
            }
            else
            {
                // First arg is output
                if (calleeVar == null)
                {
                    // out in
                    foreach (var caller in m.DefinedTasks)
                        if (m.TaskCalls(caller, callee))
                            if (k(o, BindingList<LogicVariable>.Bind(e.Unifications, callerVar, caller), e.State,
                                predecessor))
                                return true;
                }
                else
                {
                    // out out
                    foreach (var caller in m.DefinedTasks)
                        foreach (var c in m.Callees(caller))
                            if (k(o,
                                BindingList<LogicVariable>.Bind(e.Unifications, callerVar, caller).Bind(calleeVar, c),
                                e.State, predecessor))
                                return true;
                }
            }

            return false;
        }

        private static bool TaskSubtask(object[] args, PartialOutput o, BindingEnvironment e, Step.Continuation k, MethodCallFrame predecessor)
        {
            ArgumentCountException.Check("TaskSubtask", 2, args);
            var task = ArgumentTypeException.Cast<CompoundTask>("TaskSubtask", args[0], args);
            foreach (var callExpression in e.Module.Subtasks(task))
                if (e.Unify(args[1], callExpression, out var unifications))
                    if (k(o, unifications, e.State, predecessor))
                        return true;
            return false;
        }
    }
}
