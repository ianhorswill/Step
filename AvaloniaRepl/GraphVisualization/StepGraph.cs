using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Threading;
using AvaloniaRepl.Views;
using GraphViz;
using Step;
using Step.Interpreter;
using Step.Output;


namespace AvaloniaRepl.GraphVisualization
{
    internal static class StepGraph
    {
        public static GeneralPrimitive VisualizeGraph = new GeneralPrimitive(nameof(VisualizeGraph), VisualizeGraphImplementation);

        public static void AddPrimitives(Module m)
        {
            m[nameof(VisualizeGraph)] = VisualizeGraph;
        }

        public static void ShowCallGraph() => ShowGraph("Call graph", CallGraph());

        public static void ShowGraph(string name, Graph graph)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var visualization = new Views.GraphVisualization() { DataContext = graph };
                MainWindow.Instance.AddTab(name, visualization, true);
            });
        }

        private static Graph<string> CallGraph()
        {
            var g = new GraphViz.Graph<string>();
            foreach (var t in StepCode.Module.DefinedTasks)
            foreach (var callee in t.Callees)
            {
                switch (callee)
                {
                    case Task t2:
                        g.AddEdge(new Graph<string>.Edge(t.Name, t2.Name, true), true);
                        break;

                    case StateVariableName v:
                        g.AddEdge(new Graph<string>.Edge(t.Name, v.Name, true), true);
                        break;

                    default:
                        if (callee == null)
                            continue;
                        g.AddEdge(new Graph<string>.Edge(t.Name, callee.ToString(), true), true);
                        break;
                }
            }
            g.RecolorByComponent();

            return g;
        }

        private static bool VisualizeGraphImplementation(object?[] args, TextBuffer o, BindingEnvironment e,
            MethodCallFrame? predecessor, Step.Interpreter.Step.Continuation k)
        {
            ArgumentCountException.CheckAtLeast(VisualizeGraph, 1, args);
            var edges = ArgumentTypeException.Cast<Task>(VisualizeGraph, args[0], args);
            Task nodes = null;
            var directed = false;
            var windowName = "Graph";

            var graph = new Graph<object>(Term.Comparer.Default);
            graph.NodeLabel = x => Writer.TermToString(x, null);

            for (var i = 1; i < args.Length; i += 2)
            {
                var keyword = ArgumentTypeException.Cast<string>(VisualizeGraph, args[i], args);
                if (args.Length == i + 1)
                    throw new ArgumentCountException(VisualizeGraph, args.Length + 1, args);
                var value = args[i + 1];

                switch (keyword)
                {
                    case "nodes":
                        nodes = ArgumentTypeException.Cast<Task>(VisualizeGraph, value, args);
                        break;

                    case "directed":
                        directed = ArgumentTypeException.Cast<bool>(VisualizeGraph, value, args);
                        break;

                    case "name":
                        windowName = StringifyStepObject(value);
                        break;

                    default:
                        throw new ArgumentException($"Unknown keyword {keyword} in call to {VisualizeGraph.Name}");
                }
            }

            var startNodeVar = new LogicVariable("?startNode", 0);
            var endNodeVar = new LogicVariable("?endNode", 1);
            var labelVar = new LogicVariable("?label", 2);
            var colorVar = new LogicVariable("?color", 2);
            var directedVar = new LogicVariable("?directed", 2);

            var edgeArgCount = edges.ArgumentCount??2;
            var edgeArgs = edgeArgCount switch
            {
                2 => new object[] { startNodeVar, endNodeVar },
                3 => new object[] { startNodeVar, endNodeVar, labelVar },
                4 => new object[] { startNodeVar, endNodeVar, labelVar, colorVar },
                5 => new object[] { startNodeVar, endNodeVar, labelVar, colorVar, directedVar },
                _ => throw new ArgumentException($"First argument to {nameof(VisualizeGraph)}, {Writer.TermToString(edges)}, must be a task that accepts 2-5 arguments.")
            };
            
            edges.Call(edgeArgs, o, e, predecessor, (no, u, s, f) =>
            {
                var env = new BindingEnvironment(e, u, s);
                var start = env.Resolve(startNodeVar);
                var end = env.Resolve(endNodeVar);
                var label = edgeArgCount>2?StringifyStepObject(env.Resolve(labelVar)):null;
                var color = edgeArgCount > 3 ? env.Resolve(colorVar) as string : null;
                var thisEdgeDirected = edgeArgCount > 4 ? (bool)env.Resolve(directedVar) : directed;
                var edge = new Graph<object>.Edge(
                    start, end, 
                    thisEdgeDirected,
                    label,
                    color != null?new Dictionary<string, object>() { { "color", color } }:null);
                graph.AddEdge(edge, 
                    true);
                return false; // backtrack
            });
            ShowGraph(windowName, graph);
            return true;
        }


        public static Graph GenerateGraphFromTasks(Task nodeGenerator, Task edgeGenerator, State? state = null)
        {
            // Generate nodes
            if (state == null) state = State.Empty;
            var g = new Graph<object?>(Term.Comparer.Default);
            g.NodeLabel = StringifyStepObject;


            var nodes = new List<object>();
            var answer = new LogicVariable("?answer", 0);
            var env = new BindingEnvironment(StepCode.Module, null, null, state.Value);
            var textBuffer = new TextBuffer(0);
            nodeGenerator.Call(new[] { answer }, textBuffer,
                env, null,
                (t,u,s,p ) =>
                {
                    nodes.Add(new BindingEnvironment(env, u, s).CopyTerm(answer));
                    return false;
                });

            // Add nodes
            foreach (var n in nodes) 
                g.AddNode(n);

            // Generate and add edges
            foreach (var n in nodes)
            {
                edgeGenerator.Call(new [] { n, answer }, textBuffer, env, null,
                    (t,u,s,p ) =>
                    {
                        var neighbor = new BindingEnvironment(env, u, s).CopyTerm(answer);
                        g.AddEdge(new Graph<object>.Edge(n, neighbor, true));
                        return false;
                    });
            }

            return g;
        }

        public static string StringifyStepObject(object? o)
        {
            return o switch
            {
                null => "null",
                string[] text => text.Untokenize(),
                object?[] tuple => Writer.TermToString(tuple),
                _ => o.ToString()
            };
        }
    }
}
