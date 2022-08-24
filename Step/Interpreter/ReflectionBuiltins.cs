using System.Collections.Generic;
using System.Linq;
using Step.Utilities;

namespace Step.Interpreter
{
    static class ReflectionBuiltins
    {
        public static void DefineGlobals()
        {
            var g = Module.Global;

            Documentation.SectionIntroduction("reflection",
                "Predicates that can be used by a program to reason about itself.");

            Documentation.SectionIntroduction("reflection//static analysis",
                "Predicates that can be used by a program to check what tasks can call what other tasks.");

            Documentation.SectionIntroduction("reflection//dynamic analysis",
                "Predicates that can be used by a program to check what tasks have been called in this execution path.");

            // Argument is a compound task
            g["CompoundTask"] = new GeneralPrimitive(nameof(CompoundTask), CompoundTask)
                .Arguments("x")
                .Documentation("type testing", "True if x is a compound task, i.e. a task defined by rules.");

            // Second arg is a method of the first arg
            bool InInMode(CompoundTask t, Method m) => m.Task == t;
            IEnumerable<Method> InOutMode(CompoundTask t) => t.Methods;
            IEnumerable<CompoundTask> OutInMode(Method m) => new[] {m.Task};
            g["TaskMethod"] =
                new GeneralPredicate<CompoundTask, Method>("TaskMethod", InInMode, InOutMode, OutInMode, null)
                    .Arguments("?task", "?method")
                    .Documentation("reflection//static analysis", "True when ?method is a method of ?task");

            // Gets the MethodCallFrame of the most recent call
            g["LastMethodCallFrame"] = new GeneralPrimitive("LastMethodCallFrame", LastMethodCallFrame)
                .Arguments("?frame")
                .Documentation("reflection//dynamic analysis", "Sets ?frame to the reflection information for the current method call.");

            // Second argument is in the caller chain leading to the first argument
            bool InInMode1(MethodCallFrame f, Method m) => f.CallerChain.FirstOrDefault(a => a.Method == m) != null;
            IEnumerable<Method> InOutMode1(MethodCallFrame f) => f.CallerChain.Select(a => a.Method!);
            g["CallerChainAncestor"] = 
                new GeneralPredicate<MethodCallFrame, Method>("CallerChainAncestor", InInMode1, InOutMode1, null, null)
                    .Arguments("frame", "?method")
                    .Documentation("reflection//dynamic analysis", "True if ?method called frame's method or some other method that eventually called this frame's method.");

            // Second argument is in the goal chain leading to the first argument
            bool InInMode2(MethodCallFrame f, Method m) => MethodCallFrame.GoalChain(f).FirstOrDefault(a => a.Method == m) != null;
            IEnumerable<Method> InOutMode2(MethodCallFrame f) => MethodCallFrame.GoalChain(f).Select(a => a.Method!);
            g["GoalChainAncestor"] = 
                new GeneralPredicate<MethodCallFrame, Method>("GoalChainAncestor", InInMode2, InOutMode2, null, null)
                    .Arguments("frame", "?method")
                    .Documentation("reflection//dynamic analysis", "True if a successful call to ?method preceded this frame.");

            // First argument calls the second argument
            g["TaskCalls"] = new GeneralPrimitive(nameof(TaskCalls), TaskCalls)
                .Arguments("?caller", "?callee")
                .Documentation("reflection//static analysis", "True if task ?caller has a method that calls ?callee");

            // Second argument is a call expression for a call in some method of first argument.
            g["TaskSubtask"] = new GeneralPrimitive(nameof(TaskSubtask), TaskSubtask)
                .Arguments("?task", "?call")
                .Documentation("reflection//static analysis", "True if task ?caller has a method that contains the call ?call.");
        }

        private static bool LastMethodCallFrame(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check("LastMethodCallFrame", 1, args);
            return e.Unify(args[0], predecessor, out var u)
                   && k(o, u, e.State, predecessor);
        }

        private static bool CompoundTask(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check("CompoundTask", 1, args);
            var arg = e.Resolve(args[0]);
            var l = arg as LogicVariable;
            if (l == null)
                // Argument is instantiated; test if it's a compound task
                return (arg is CompoundTask) && k(o, e.Unifications, e.State, predecessor);
            foreach (var t in e.Module.DefinedTasks)
                if (k(o, BindingList.Bind(e.Unifications, l, t), e.State, predecessor))
                    return true;

            return false;
        }

        private static bool TaskCalls(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
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
                    if (m.TaskCalls(callerTask!, callee!))
                        return k(o, e.Unifications, e.State, predecessor);
                }
                else
                {
                    // in out
                    foreach (var c in m.Callees(callerTask!))
                        if (k(o, BindingList.Bind(e.Unifications, calleeVar, c), e.State, predecessor))
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
                        if (m.TaskCalls(caller, callee!))
                            if (k(o, BindingList.Bind(e.Unifications, callerVar, caller), e.State,
                                predecessor))
                                return true;
                }
                else
                {
                    // out out
                    foreach (var caller in m.DefinedTasks)
                        foreach (var c in m.Callees(caller))
                            if (k(o,
                                BindingList.Bind(e.Unifications, callerVar, caller).Bind(calleeVar, c),
                                e.State, predecessor))
                                return true;
                }
            }

            return false;
        }

        private static bool TaskSubtask(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Step.Continuation k)
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
