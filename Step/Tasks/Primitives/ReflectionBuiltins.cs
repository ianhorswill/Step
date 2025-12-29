using System;
using System.Collections.Generic;
using System.Linq;
using Step.Binding;
using Step.Exceptions;
using Step.Interpreter;
using Step.Interpreter.Steps;
using Step.Output;
using Step.Terms;
using Step.Utilities;

namespace Step.Tasks.Primitives
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

            g["TaskSubtask"] = new GeneralPrimitive(nameof(TaskSubtask), TaskSubtask)
                .Arguments("?task", "?call")
                .Documentation("reflection//static analysis", "True if task ?caller has a method that contains the call ?call.");

            g["Method"] = new GeneralPrimitive(nameof(Method), Method)
                .Arguments("?call", "?methodBody")
                .Documentation("reflection//static analysis", "True if ?methodBody is a call equivalent to the body of a rule that matches ?call.");

            g[nameof(TaskProperty)] = new GeneralPrimitive(nameof(TaskProperty), TaskProperty)
                .Arguments("?task", "?property", "?value")
                .Documentation("reflection//static analysis", "True if ?task has property ?property with value ?value.");

            g[nameof(MetaTask)] = new GeneralPrimitive(nameof(MetaTask), MetaTask)
                .Arguments("?task", "?metaTask")
                .Documentation("reflection//static analysis", "True if ?task has metatask ?metatask.");
        }

        private static bool LastMethodCallFrame(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Task.Continuation k)
        {
            ArgumentCountException.Check("LastMethodCallFrame", 1, args, o);
            return e.Unify(args[0], predecessor, out BindingList? u)
                   && k(o, u, e.State, predecessor);
        }

        private static bool CompoundTask(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Task.Continuation k)
        {
            ArgumentCountException.Check("CompoundTask", 1, args, o);
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

        private static bool TaskCalls(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Task.Continuation k)
        {
            var m = e.Module;
            ArgumentCountException.Check(nameof(TaskCalls), 2, args, o);
            ArgumentTypeException.Check(nameof(TaskCalls), typeof(CompoundTask), args[0], args, o, true);
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
                        if (c is Task && k(o, BindingList.Bind(e.Unifications, calleeVar, c), e.State, predecessor))
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
                        if (caller.Name != "TopLevelCall")
                            foreach (var c in m.Callees(caller))
                                if (k(o,
                                        BindingList.Bind(e.Unifications, callerVar, caller).Bind(calleeVar, c),
                                        e.State, predecessor))
                                    return true;
                }
            }

            return false;
        }

        private static bool TaskSubtask(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor, Task.Continuation k)
        {
            ArgumentCountException.Check("TaskSubtask", 2, args, o);
            var task = ArgumentTypeException.Cast<CompoundTask>("TaskSubtask", args[0], args, o);
            foreach (var callExpression in e.Module.Subtasks(task))
                if (e.Unify(args[1], callExpression, out BindingList? unifications))
                    if (k(o, unifications, e.State, predecessor))
                        return true;
            return false;
        }
        private static bool Method(object?[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame? predecessor,
            Task.Continuation k)
        {
            ArgumentCountException.Check(nameof(Method), 2, args, o);
            Task task;
            object? taskArgs;
            var call = args[0];
            if (call is Pair cell)
            {
                task = ArgumentTypeException.Cast<Task>(nameof(Method), cell.First, args, o);
                taskArgs = cell.Rest;
            }
            else
            {
                var array = ArgumentTypeException.Cast<object?[]>(nameof(Method), args[0], args, o);
                if (array.Length == 0)
                    throw new ArgumentTypeException(nameof(Method), typeof(object?[]), call, args, o);
                task = ArgumentTypeException.Cast<Task>(nameof(Method), array[0], args, o);
                taskArgs = array.Skip(1).ToArray();
            }
            
            if (task is PrimitiveTask)
            {
                object?[] argArray = taskArgs switch
                {
                    object?[] a => a,
                    Pair cell1 when Pair.CompressPairChainsWhenPossible(cell1, e.Unifications) is object?[] array => array,
                    _ => throw new ArgumentException($"Task arguments in call to Method are not a proper list: {taskArgs}")
                };

                // Call the task, then report the method is either "true" or "false".
                return task.Call(argArray, o, e, predecessor,
                    (newOutput, newUnif, newState, newPred) =>
                        e.Unify(args[1], Array.Empty<object?>(), newUnif, out var finalUniv)
                        && k(newOutput, finalUniv, newState, newPred));
            } else if (task is CompoundTask t)
            {
                var buffer = new List<object?>();
                foreach (var m in t.Methods)
                {
                    //var head = m.ArgumentPattern;
                    var env = m.TryMatch(taskArgs, e, predecessor);
                    if (env != null)
                    {

                        buffer.Clear();
                        for (var step = m.StepChain; step != null; step = step.Next)
                            switch (step)
                            {
                                case Call c:
                                    buffer.Add(env.Value.ResolveList(c.Arglist.Prepend(c.Task).ToArray()));
                                    break;

                                case EmitStep emit:
                                    buffer.Add(new object?[] { Builtins.WritePrimitive, emit.Text });
                                    break;

                                default:
                                    throw new InvalidOperationException($"Cannot translate {step} into a call");
                            }

                        if (env.Value.Unify(args[1], buffer.ToArray(), env.Value.Unifications, out var final)
                            && k(o, final, env.Value.State, env.Value.Frame))
                            return true;
                    }
                }
                // No remaining methods match
                return false;
            }
            else 
                throw new InvalidOperationException($"Task in call to Method is neither primitive nor compound.");
        }

        private static bool TaskProperty(object?[] args, TextBuffer o, BindingEnvironment e,
            MethodCallFrame? predecessor,
            Task.Continuation k)
        {
            ArgumentCountException.Check(nameof(TaskProperty), 3, args, o);
            var task = ArgumentTypeException.Cast<Task>(nameof(TaskProperty), args[0], args, o);
            var property = args[1];
            if (property is LogicVariable l)
            {
                foreach (var prop in task.Properties)
                {
                    if (e.UnifyArrays([l, args[2]], [prop.Key, prop.Value],
                            out BindingList? bindings) && k(o, bindings, e.State, predecessor))
                        return true;
                }

                return false;
            } else 
                return property != null 
                       && task.Properties.ContainsKey(property) &&
                       e.Unify(task.GetProperty<object>(property, e.Module), args[2], out BindingList? bindings) &&
                       k(o, bindings, e.State, predecessor);
        }

        private static bool MetaTask(object?[] args, TextBuffer o, BindingEnvironment e,
            MethodCallFrame? predecessor,
            Task.Continuation k)
        {
            ArgumentCountException.Check(nameof(MetaTask), 2, args, o);
            var mtArg = args[1];
            switch (args[0])
            {
                case CompoundTask t:
                {
                    if (mtArg is Task mt)
                        // in in
                        return t.MetaTask == mt && k(o, e.Unifications, e.State, predecessor);
                    else
                        // in out
                        return e.Unify(mtArg, t.MetaTask, out BindingList? u) && k(o, u, e.State, predecessor);
                }

                case LogicVariable tl:
                {
                    switch (mtArg)
                    { 
                        case Task mt:
                        {
                            // out in
                            foreach (var t in e.Module.DefinedTasks)
                                if (t.MetaTask == mt && e.Unify(tl, t, out BindingList? u) && k(o, u, e.State, predecessor))
                                    return true;
                            return false;
                        }
                        case LogicVariable mtl:
                        {
                            // out out
                            foreach (var t in e.Module.DefinedTasks)
                                if (t.MetaTask != null && e.Unify(tl, t, out BindingList? u) && e.Unify(mtl, t.MetaTask, u, out var u2)
                                    && k(o, u2, e.State, predecessor))
                                    return true;
                            return false;
                        }
                        default:
                            ArgumentTypeException.Check(nameof(MetaTask), typeof(Task), mtArg, args, o);
                            break;
                        }

                }
                    break;

                default: 
                    ArgumentTypeException.Check(nameof(MetaTask), typeof(CompoundTask), args[0], args, o);
                    break;
            }

            return false;
        }
    }
}
