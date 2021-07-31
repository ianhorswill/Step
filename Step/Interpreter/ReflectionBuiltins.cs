﻿using System;
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
            g["CompoundTask"] = new GeneralPrimitive(nameof(CompoundTask), CompoundTask);

            // Second arg is a method of the first arg
            Func<CompoundTask, Method, bool> inInMode = (t, m) => m.Task == t;
            Func<CompoundTask, IEnumerable<Method>> inOutMode = t => t.Methods;
            Func<Method, IEnumerable<CompoundTask>> outInMode = m => new[] {m.Task};
            g["TaskMethod"] =
                new GeneralPredicate<CompoundTask, Method>("TaskMethod", inInMode, inOutMode, outInMode, null);

            // Gets the MethodCallFrame of the most recent call
            g["LastMethodCallFrame"] = new GeneralPrimitive("LastMethodCallFrame", LastMethodCallFrame);

            // Second argument is in the caller chain leading to the first argument
            Func<MethodCallFrame, Method, bool> inInMode1 = (f, m) => f.CallerChain.FirstOrDefault(a => a.Method == m) != null;
            Func<MethodCallFrame, IEnumerable<Method>> inOutMode1 = f => f.CallerChain.Select(a => a.Method);
            g["CallerChainAncestor"] = 
                new GeneralPredicate<MethodCallFrame, Method>("CallerChainAncestor", inInMode1, inOutMode1, null, null);

            // Second argument is in the goal chain leading to the first argument
            Func<MethodCallFrame, Method, bool> inInMode2 = (f, m) => f.GoalChain.FirstOrDefault(a => a.Method == m) != null;
            Func<MethodCallFrame, IEnumerable<Method>> inOutMode2 = f => f.GoalChain.Select(a => a.Method);
            g["GoalChainAncestor"] = 
                new GeneralPredicate<MethodCallFrame, Method>("GoalChainAncestor", inInMode2, inOutMode2, null, null);

            // First argument calls the second argument
            g["TaskCalls"] = new GeneralPrimitive(nameof(TaskCalls), TaskCalls);

            // Second argument is a call expression for a call in some method of first argument.
            g["TaskSubtask"] = new GeneralPrimitive(nameof(TaskSubtask), TaskSubtask);
        }

        private static bool LastMethodCallFrame(object[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame predecessor, Step.Continuation k)
        {
            ArgumentCountException.Check("LastMethodCallFrame", 1, args);
            return e.Unify(args[0], predecessor, out var u)
                   && k(o, u, e.State, predecessor);
        }

        private static bool CompoundTask(object[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame predecessor, Step.Continuation k)
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

        private static bool TaskCalls(object[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame predecessor, Step.Continuation k)
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

        private static bool TaskSubtask(object[] args, TextBuffer o, BindingEnvironment e, MethodCallFrame predecessor, Step.Continuation k)
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
