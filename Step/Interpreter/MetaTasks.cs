using Step.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Step.Output;

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
            g[nameof(PartialOrderInfo.FastPartialOrder)] = new GeneralPrimitive(nameof(PartialOrderInfo.FastPartialOrder), PartialOrderInfo.FastPartialOrder)
                .Arguments("call")
                .Documentation("control flow//metatasks",
                    "Used as a metatask for a two-argument predicate to make it a partial order.  This will build a complete table of the transitive closure of the predicate in advance, so it should only be used for non-fluent predicates whose arguments are atoms.  Predicate must be anti-reflexive.");
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

        private static readonly LocalVariableName Temp = new LocalVariableName("?temp", -1);
        private static readonly LocalVariableName Left = new LocalVariableName("?left", -1);
        private static readonly LocalVariableName Right = new LocalVariableName("?right", -1);

        private static bool PartialOrder(object?[] args, TextBuffer output, BindingEnvironment env,
            MethodCallFrame? p, Step.Continuation k)
        {
            var (task, tArgs) = CheckBinaryRelation(nameof(PartialOrder), args, output, env);
            
            var a = tArgs[0];
            var b = tArgs[1];

            var enumerateMode = a is LogicVariable && b is LogicVariable;

            if (enumerateMode)
            {
                var left = new LogicVariable(Left);
                var right = new LogicVariable(Right);
                if (task.CallDirect([left, right], output, env, p,
                        (o, b1, s, p2) =>
                        {
                            var ne = new BindingEnvironment(env, b1, s);
                            return (ne.UnifyArrays([left, right], [a, b], out BindingList? b2)
                                    && k(o, b2, s, p2))
                                   || (ne.UnifyArrays([left, left], [a, b], out BindingList? b3)
                                       && k(o, b3, s, p2))
                                   || (ne.UnifyArrays([right, right], [a, b], out BindingList? b4)
                                       && k(o, b4, s, p2));
                        }))
                    return true;
            }

            bool Recur(object? a, object? b, TextBuffer output, BindingEnvironment e)
            {
                // Reflexive case
                if (!enumerateMode && env.Unify(a, b, e.Unifications, out var bindings)
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

            return Recur(a, b, output, env);
        }

        private class PartialOrderInfo
        {
            private readonly Dictionary<object, List<object?>> upperBounds = new();
            private readonly Dictionary<object, List<object?>> lowerBounds = new();

            private void AddBounds(object? lesser, object? greater)
            {
                if (!upperBounds.TryGetValue(lesser!, out var uppers))
                {
                    uppers = new List<object?>();
                    upperBounds[lesser!] = uppers;
                }

                if (!uppers.Contains(greater))
                    uppers.Add(greater);
                if (!lowerBounds.TryGetValue(greater!, out var lowers))
                {
                    lowers = new List<object?>();
                    lowerBounds[greater!] = lowers;
                }
                if (!lowers.Contains(lesser))
                    lowers.Add(lesser);
            }

            private static PartialOrderInfo Generate(CompoundTask task, TextBuffer output, BindingEnvironment env, MethodCallFrame? p)
            {
                var info = new PartialOrderInfo();
                var left = new LogicVariable(Left);
                var right = new LogicVariable(Right);
                object?[] call = [task, left, right];
                // Find all solutions to [task ?left ?right] and add them to the bounds tables.
                PartialOrder([call], output, env, p, (o, bindings, state, _) =>
                {
                    var e = new BindingEnvironment(env, bindings, state);
                    var l = e.Resolve(left);
                    var r = e.Resolve(right);
                    if (l is LogicVariable || r is LogicVariable)
                        throw new StepExecutionException(
                            $"While compiling bounds tables for {task}, {task} generated the solution {Writer.TermToString(call, bindings)}, which contains an unbound variable.",
                            output);
                    info.AddBounds(l, r);
                    return false;
                });
                return info;
            }

            public static PartialOrderInfo BoundsFor(CompoundTask task, TextBuffer output, BindingEnvironment env,
                MethodCallFrame? p)
            {
                var info = task.GetPropertyOrDefault<PartialOrderInfo?>(typeof(PartialOrderInfo));
                if (info == null)
                {
                    info = Generate(task, output, env, p);
                    task.SetPropertyValue(typeof(PartialOrderInfo), info);
                }

                return info;
            }

            public static bool FastPartialOrder(object?[] args, TextBuffer output, BindingEnvironment env,
                MethodCallFrame? p, Step.Continuation k)
            {
                var (task, tArgs) = CheckBinaryRelation(nameof(FastPartialOrder), args, output, env);
                var info = BoundsFor((CompoundTask)task, output, env, p);

                var left = tArgs[0];
                var right = tArgs[1];
                var lv = left as LogicVariable;
                var rv = right as LogicVariable;
                if (lv == null)
                {
                    // left is bound
                    if (left == null || !info.upperBounds.TryGetValue(left, out var ub))
                        return false;

                    if (rv == null)
                        // both are bound
                        return ub.Contains(right) && k(output, env.Unifications, env.State, p);
                    else
                    {
                        // left bound, right unbound
                        foreach (var bound in ub)
                        {
                            if (env.Unify(right, bound, env.Unifications, out var bindings)
                                && k(output, bindings, env.State, p))
                                return true;
                        }

                        return false;
                    }
                }
                else
                {
                    // left is unbound
                    if (rv == null)
                    {
                        // left unbound, right bound
                        if (right == null || !info.lowerBounds.TryGetValue(right, out var lb))
                            return false;
                        foreach (var bound in lb)
                        {
                            if (env.Unify(left, bound, env.Unifications, out var bindings)
                                && k(output, bindings, env.State, p))
                                return true;
                        }

                        return false;
                    }
                    else
                    {
                        // both are unbound
                        foreach (var pair in info.upperBounds)
                        {
                            foreach (var bound in pair.Value)
                            {
                                if (env.Unify(left, pair.Key, env.Unifications, out var bindings1)
                                    && env.Unify(right, bound, bindings1, out var bindings2)
                                    && k(output, bindings2, env.State, p))
                                    return true;
                            }
                        }

                        return false;
                    }
                }
            }
        }
    }
}
