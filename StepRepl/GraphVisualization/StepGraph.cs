#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using StepRepl.Views;
using GraphViz;
using Step;
using Step.Interpreter;
using Step.Output;


namespace StepRepl.GraphVisualization
{
    internal static class StepGraph
    {
        public static GeneralPrimitive VisualizeGraph = new(nameof(VisualizeGraph), VisualizeGraphImplementation);

        public static void AddPrimitives(Module m)
        {
            m[nameof(VisualizeGraph)] = VisualizeGraph;
        }

        public static void ShowCallGraph() => ShowGraph("Call graph", CallGraph());

        public static void ShowGraph(string name, Graph graph)
        {
            if (StepCode.ProjectDirectory == "") return;
            
            Dispatcher.UIThread.Post(() =>
            {
                var visualization = new Views.GraphVisualization() { DataContext = graph };
                MainWindow.Instance.AddTab(name, visualization);
            });
        }

        private static Graph<string> CallGraph()
        {
            var g = new Graph<string>();
            foreach (var t in StepCode.Module.DefinedTasks)
            foreach (var callee in t.Callees)
            {
                switch (callee)
                {
                    case Task t2:
                        g.AddEdge(new Graph<string>.Edge(t.Name, t2.Name), true);
                        break;

                    case StateVariableName v:
                        g.AddEdge(new Graph<string>.Edge(t.Name, v.Name), true);
                        break;

                    default:
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                        if (callee == null)
                            continue;
                        g.AddEdge(new Graph<string>.Edge(t.Name, callee.ToString()!), true);
                        break;
                }
            }
            g.RecolorByComponent();

            return g;
        }

        private static bool VisualizeGraphImplementation(object?[] args, TextBuffer o, BindingEnvironment e,
            MethodCallFrame? predecessor, Step.Interpreter.Step.Continuation k)
        {
            ArgumentCountException.CheckAtLeast(VisualizeGraph, 1, args, o);
            var edges = ArgumentTypeException.Cast<Task>(VisualizeGraph, args[0], args, o);
            Task? nodes = null;
            Task? nodeColor = null;
            var directed = false;
            var windowName = "Graph";
            var colorByComponent = false;
            var hierarchical = false;

            var labelVar = new LogicVariable("?label", 2);

            var graph = new Graph<object>(Term.Comparer.Default)
            {
                NodeLabel = x => Writer.TermToString(x)
            };

            for (var i = 1; i < args.Length; i += 2)
            {
                var keyword = ArgumentTypeException.Cast<string>(VisualizeGraph, args[i], args, o);
                if (args.Length == i + 1)
                    throw new ArgumentCountException(VisualizeGraph, args.Length + 1, args, o);
                var value = args[i + 1];

                switch (keyword)
                {
                    case "nodes":
                    case "roots":
                        nodes = ArgumentTypeException.Cast<Task>(VisualizeGraph, value, args, o);
                        break;

                    case "node_color":
                        nodeColor = ArgumentTypeException.Cast<Task>(VisualizeGraph, value, args, o);
                        break;

                    case "node_label":
                    {
                        var nodeLabel = ArgumentTypeException.Cast<Task>(VisualizeGraph, value, args, o);
                        graph.NodeLabel = node =>
                        {
                            var label = "unknown label";
                            nodeLabel.Call(new object?[] { node, labelVar }, o, e, predecessor, (_, u, s, _) =>
                            {
                                var env = new BindingEnvironment(e, u, s);
                                label = StringifyStepObject(env.Resolve(labelVar));
                                return true;
                            });
                            return label;
                        };
                    }
                        break;

                    case "directed":
                        directed = ArgumentTypeException.Cast<bool>(VisualizeGraph, value, args, o);
                        break;

                    case "name":
                        windowName = StringifyStepObject(value);
                        break;

                    case "color_components":
                        colorByComponent = true;
                        break;

                    case "hierarchical":
                        hierarchical = ArgumentTypeException.Cast<bool>(VisualizeGraph, value, args, o);
                        graph.Hierarchical = hierarchical;
                        break;

                    default:
                        throw new ArgumentException($"Unknown keyword {keyword} in call to {VisualizeGraph.Name}");
                }
            }

            var startNodeVar = new LogicVariable("?startNode", 0);
            var endNodeVar = new LogicVariable("?endNode", 1);
            var colorVar = new LogicVariable("?color", 2);
            var directedVar = new LogicVariable("?directed", 2);

            var edgeArgCount = edges.ArgumentCount??2;
            var edgeArgs = edgeArgCount switch
            {
                2 => [startNodeVar, endNodeVar],
                3 => [startNodeVar, endNodeVar, labelVar],
                4 => [startNodeVar, endNodeVar, labelVar, colorVar],
                5 => new object[] { startNodeVar, endNodeVar, labelVar, colorVar, directedVar },
                _ => throw new ArgumentException($"First argument to {nameof(VisualizeGraph)}, {Writer.TermToString(edges)}, must be a task that accepts 2-5 arguments.")
            };

            if (hierarchical)
            {
                if (nodes == null)
                    throw new ArgumentException(
                        $"{nameof(VisualizeGraph)}: must specify a roots argument when in hierarchical mode.");
                var q = new Queue<object>();
                var nodeVar = new LogicVariable("?node", 0);
                var nodesArgs = new object?[] { nodeVar };
                nodes.Call(nodesArgs, o, e, predecessor, (_, nu, s, _) =>
                {
                    var nenv = new BindingEnvironment(e, nu, s);
                    var root = nenv.Resolve(nodeVar, nenv.Unifications, true)!;
                    if (!graph.Nodes.Contains(root))
                    {
                        graph.Rank[root] = 0;
                        graph.AddNode(root);
                        q.Enqueue(root);
                        while (q.Count > 0)
                        {
                            var node = q.Dequeue();
                            var rank = graph.Rank[node];

                            var childArgs = edgeArgs.ToArray();
                            childArgs[0] = node;

                            // Follow edges of node
                            edges.Call(childArgs, o, nenv, predecessor, (_, u, s, _) =>
                            {
                                var env = new BindingEnvironment(nenv, u, nenv.State);
                                var start = node;
                                var end = env.Resolve(endNodeVar, env.Unifications, true);

                                if (!graph.Nodes.Contains(end))
                                {
                                    graph.Rank[end] = rank + 1;
                                    graph.AddNode(end);
                                    q.Enqueue(end);
                                }

                                var label = edgeArgCount > 2
                                    ? StringifyStepObject(env.Resolve(labelVar, env.Unifications, true))
                                    : null;
                                var color = edgeArgCount > 3 ? env.Resolve(colorVar) as string : null;
                                var thisEdgeDirected = edgeArgCount > 4
                                    ? (bool)env.Resolve(directedVar, env.Unifications, true)!
                                    : directed;
                                var edge = new Graph<object>.Edge(
                                    start!, end!,
                                    thisEdgeDirected,
                                    label,
                                    color != null ? new Dictionary<string, object>() { { "color", color } } : null);
                                graph.AddEdge(edge,
                                    true);
                                return false; // backtrack
                            });
                        }
                    }

                    return false;
                });
            } 
            else
            {
                // Add all the edges
                edges.Call(edgeArgs, o, e, predecessor, (_, u, s, _) =>
                {
                    var env = new BindingEnvironment(e, u, s);
                    var start = env.Resolve(startNodeVar, env.Unifications, true);
                    var end = env.Resolve(endNodeVar, env.Unifications, true);
                    var label = edgeArgCount > 2
                        ? StringifyStepObject(env.Resolve(labelVar, env.Unifications, true))
                        : null;
                    var color = edgeArgCount > 3 ? env.Resolve(colorVar) as string : null;
                    var thisEdgeDirected = edgeArgCount > 4
                        ? (bool)env.Resolve(directedVar, env.Unifications, true)!
                        : directed;
                    var edge = new Graph<object>.Edge(
                        start!, end!,
                        thisEdgeDirected,
                        label,
                        color != null ? new Dictionary<string, object>() { { "color", color } } : null);
                    graph.AddEdge(edge,
                        true);
                    return false; // backtrack
                });

                // Add any nodes not added as part of the edges
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (nodes != null)
                {
                    var nodeVar = new LogicVariable("?node", 0);
                    var nodesArgs = new object?[] { nodeVar };
                    nodes.Call(nodesArgs, o, e, predecessor, (_, u, s, _) =>
                    {
                        var env = new BindingEnvironment(e, u, s);
                        var node = env.Resolve(nodeVar, env.Unifications, true)!;
                        if (!graph.Nodes.Contains(node))
                            graph.AddNode(node);
                        return false;
                    });
                }
            }

            // Assign colors for nodes
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (nodeColor != null)
            {
                var colorArgs = new object?[] { null, colorVar };
                foreach (var node in graph.Nodes )
                {
                    colorArgs[0] = node;
                    nodeColor.Call(colorArgs, o, e, predecessor, (_, u, s, _) =>
                    {
                        var env = new BindingEnvironment(e, u, s);
                        var color = env.Resolve(colorVar)!;
                        graph.NodeAttributes[node]["fillcolor"] = color;
                        return true;
                    });
                }
            }


            if (colorByComponent)
                graph.RecolorByComponent();
            ShowGraph(windowName, graph);
            return k(o, e.Unifications, e.State, predecessor);
        }


        public static Graph GenerateGraphFromTasks(Task nodeGenerator, Task edgeGenerator, State? state = null)
        {
            // Generate nodes
            if (state == null) state = State.Empty;
            var g = new Graph<object>(Term.Comparer.Default)
            {
                NodeLabel = StringifyStepObject
            };

            var nodes = new List<object>();
            var answer = new LogicVariable("?answer", 0);
            var env = new BindingEnvironment(StepCode.Module, null!, null, state.Value);
            var textBuffer = new TextBuffer(0);
            nodeGenerator.Call(new object?[] { answer }, textBuffer,
                env, null,
                (_,u,s,_ ) =>
                {
                    nodes.Add(new BindingEnvironment(env, u, s).CopyTerm(answer)!);
                    return false;
                });

            // Add nodes
            foreach (var n in nodes) 
                g.AddNode(n);

            // Generate and add edges
            foreach (var n in nodes)
            {
                edgeGenerator.Call(new [] { n, answer }, textBuffer, env, null,
                    (_,u,s,_ ) =>
                    {
                        var neighbor = new BindingEnvironment(env, u, s).CopyTerm(answer);
                        g.AddEdge(new Graph<object>.Edge(n, neighbor!));
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
                _ => o.ToString()!
            };
        }
    }
}
