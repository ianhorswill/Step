#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Graph.cs" company="Ian Horswill">
// Copyright (C) 2019, 2020 Ian Horswill
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Media;

namespace StepRepl.GraphVisualization {

    /// <summary>
    /// An interactive graph visualization packaged as a Unity UI element
    /// </summary>
    public class GraphLayout
    {
        private static readonly Random Random = new();

        public readonly GraphViz.Graph Graph;

        protected static float RandomInRange(float min, float max) => (float)(Random.NextDouble() * (max - min) + min);

        public Rect Bounds;

        /// <summary>
        /// The strength of the force that moves adjacent nodes together
        /// </summary>
        public float SpringStiffness = 1;

        /// <summary>
        /// Rate (0-1) at which nodes slow down when no forces are applied to them.
        /// </summary>
        public float NodeDamping = 0.5f;

        public float ComponentRepulsionGain = 100000;

        ///// <summary>
        ///// Degree to which nodes and edges are dimmed when some other node is selected.
        ///// 0 = completely dimmed, 1 = not dimmed.
        ///// </summary>
        
        //public float GreyOutFactor = 0.5f;

        /// <summary>
        /// How far to keep nodes from the edge of the Rect for this UI element.
        /// </summary>
        public float[] Border = [100, 100, 100, 100];

#if tooltips
        /// <summary>
        /// Text object in which to display additional information about a node, or null if no info to be displayed.
        /// </summary>
        
        /// <summary>
        /// Name of the string property of a selected node to be displayed in the ToolTop element.
        /// </summary>
        public string? ToolTipProperty;
#endif

        /// <summary>
        /// Count of edges from first node to second node, for multi-graphs.
        /// </summary>
        private readonly Dictionary<(object, object), int> edgeCount = new();

        public GraphLayout(GraphViz.Graph g, Rect bounds)
        {
            Graph = g;
            Bounds = bounds;
            foreach (var nodeInfo in Graph.NodesUntyped)
            {
                var n = GetNode(nodeInfo.Node);
                IBrush b = Brushes.White;
                if ((nodeInfo.Attributes.TryGetValue("fillcolor", out var v) || Graph.GlobalNodeAttributes.TryGetValue("fillcolor", out v)) && v is string colorName)
                {
                    b = GetColorBrushByName(colorName);
                }

                n.Brush = b;
                n.Label = nodeInfo.Label;
            }

            foreach (var edgeInfo in Graph.EdgesUntyped)
            {
                AddEdge(GetNode(edgeInfo.From), GetNode(edgeInfo.To), edgeInfo.IsDirected, edgeInfo.Label!, edgeInfo.Attributes);
            }

            topologicalDistance = new short[Nodes.Count, Nodes.Count];
            foreach (var n1 in Nodes)
            foreach (var n2 in Nodes)
            {
                var d = g.UndirectedDistance(n1.Key, n2.Key);
                topologicalDistance[n1.Index, n2.Index] = float.IsPositiveInfinity(d)?short.MaxValue:(short)d;
            }
            PlaceComponents(bounds);

            TargetEdgeLength = 0.7f*(float)Math.Sqrt(bounds.Width * bounds.Height) / Graph.Diameter;
        }

        private IBrush GetColorBrushByName(string colorName) => new SolidColorBrush(GetColorByName(colorName));

        private Color GetColorByName(string colorName) => Color.Parse(colorName);

        #region Node and edge data structures

        public class GraphNode
        {
            public object Key = null!;
            public string? Label;
            public int Component = 0;
            public List<GraphEdge> AdjacentEdges = new();
            public IBrush Brush = null!;
            public Vector2 Position;
            public Vector2 PreviousPosition;
            public Vector2 NetForce;
            public int Index;
            public bool IsBeingDragged;

            public void SnapTo(Vector2 position)
            {
                Position = PreviousPosition = position;
            }
        }

        public class GraphEdge
        {
            public readonly GraphNode Start;
            public readonly GraphNode End;
            public readonly string? Label;
            public readonly IPen Pen;
            public readonly bool IsDirected;
            public readonly float TargetLength;
            public readonly int RenderPosition;

            public GraphEdge(GraphNode start, GraphNode end, string? label, Color color, bool isDirected, float targetLength, int renderPosition)
            {
                Start = start;
                End = end;
                Label = label;
                Pen = new Pen(new SolidColorBrush(color));
                IsDirected = isDirected;
                TargetLength = targetLength;
                RenderPosition = renderPosition;
            }
        }
        
        /// <summary>
        /// All GraphNode objects in this Graph, one per node/key
        /// </summary>
        public readonly List<GraphNode> Nodes = new();

        private GraphNode GetNode(object key)
        {
            var n = Nodes.Find(x => x.Key == key);
            if (n == null)
            {
                n = new GraphNode() { Key = key, Index = Nodes.Count };
                Nodes.Add(n);
            }
            return n;
        }
        
        /// <summary>
        /// All GraphEdge objects in this Graph, one per graph edge
        /// </summary>
        public readonly List<GraphEdge> Edges = new();

        private void AddEdge(GraphNode start, GraphNode end, bool isDirected, string label, Dictionary<string,object>? attributes)
        {
            Color c = Colors.White;
            if ((attributes!.TryGetValue("color", out var v) || Graph.GlobalEdgeAttributes.TryGetValue("color", out v)) && v is string colorName)
            {
                c = GetColorByName(colorName);
            }

            edgeCount.TryGetValue((start, end), out var renderPosition);
            renderPosition++;
            edgeCount[(start, end)] = renderPosition;
            var e = new GraphEdge(start, end, label, c, isDirected, 1,renderPosition);
            Edges.Add(e);
            start.AdjacentEdges.Add(e);
            end.AdjacentEdges.Add(e);
        }



        ///// <summary>
        ///// The set of pairs of nodes that are siblings, i.e. that share a connection to the same node
        ///// </summary>
        //private HashSet<(GraphNode, GraphNode)>? siblings;

        private readonly short[,] topologicalDistance;

        private int ConnectedComponentCount => Graph.ConnectedComponentCount;
        #endregion

        protected void PlaceComponents(Rect r) {
            void PlaceSingleComponent(int component, Rect rect) {
                foreach (var n in Nodes)
                    if (Graph.ConnectedComponentNumber(n.Key) == component)
                        n.Position = n.PreviousPosition = 
                            new Vector2(RandomInRange((float)rect.Left, (float)rect.Right),
                                        RandomInRange((float)rect.Top, (float)rect.Bottom));
            }

            void Place(int startComponent, int endComponent, Rect region) {
                System.Diagnostics.Debug.Assert(endComponent >= startComponent);
                if (startComponent == endComponent) { PlaceSingleComponent(startComponent, region); } else {
                    Rect r1;
                    Rect r2;
                    var p = region.Position;
                    if (region.Width > region.Height) {
                        var halfWidth = region.Width / 2;
                        var size = new Size(halfWidth, region.Height);
                        r1 = new Rect(p, size);
                        p += new Vector2((float)halfWidth,0);
                        r2 = new Rect(p, size);
                    } else {
                        var halfHeight = region.Height / 2;
                        var size = new Size(region.Width, halfHeight);
                        r1 = new Rect(p, size);
                        p += new Vector2(0, (float)halfHeight);
                        r2 = new Rect(p, size);
                    }
                    var midVector2 = (startComponent + endComponent) / 2;
                    Place(startComponent, midVector2, r1);
                    Place(midVector2 + 1, endComponent, r2);
                }
            }

            Place(0, ConnectedComponentCount - 1, r);
        }

        /// <summary>
        /// Update physics simulation of nodes
        /// </summary>
        public void FixedUpdate() {
            if (Nodes.Count == 0) return;
            UpdatePhysics();
        }

        #if ToolTips
        #region Highlighting and tooltip handling
        /// <summary>
        /// Do not use this directly.
        /// Internal field backing the SelectedNode property
        /// </summary>
        private GraphNode? selected;
        /// <summary>
        /// True if SelectedNode has changed since the last frame Update.
        /// </summary>
#pragma warning disable CS0414 // Field is assigned but its value is never used
        private bool selectionChanged;
#pragma warning restore CS0414 // Field is assigned but its value is never used
        /// <summary>
        /// Node over which the mouse is currently hovering, if any.  Else null.
        /// </summary>
        private GraphNode? SelectedNode {
            get => selected;
            set {
                if (value == selected) return;
                selected = value;
                selectionChanged = true;
            }
        }

        private static readonly Dictionary<Type, Delegate> DescriptionMethod = new();

        private static Delegate GetDescriptionMethod(object o)
        {
            for (var t = o.GetType(); t != null; t = t.BaseType)
                if (DescriptionMethod.TryGetValue(t, out var d))
                    return d;
            return null;
        }
        #endregion
#endif

        #region Physics update
        /// <summary>
        /// The "ideal" length for edges.
        /// This is the length we'd have if all the nodes were arrayed in a regular grid.
        /// </summary>
        public float TargetEdgeLength;

        public float TopBorder => (float)Bounds.Top + Border[0];
        public float RightBorder => (float)Bounds.Right - Border[0];
        public float BottomBorder => (float)Bounds.Bottom - Border[0];
        public float LeftBorder => (float)Bounds.Left + Border[0];

        public static Vector2 ZeroVector2 = new Vector2(0, 0);
        /// <summary>
        /// Compute forces on nodes and update their positions.
        /// This just updates the internal Position field of the GraphNodes.  The actual
        /// on-screen position is updated once per frame in the Update method.
        /// </summary>
        private void UpdatePhysics() {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (Nodes.Count == 0 || topologicalDistance == null) return;
            foreach (var n in Nodes) n.NetForce = ZeroVector2;

            for (var i = 0; i < Nodes.Count; i++)
                for (var j = i + 1; j < Nodes.Count; j++)
                    ApplySpringForce(i, j);

            // Keep nodes on screen
            foreach (var n in Nodes) {
                UpdatePosition(n);
                n.Position = new Vector2(Math.Clamp(n.Position.X, LeftBorder, RightBorder),
                                         Math.Clamp(n.Position.Y, TopBorder, BottomBorder));
            }
        }

        public static float FixedDeltaTime = 1.0f / 5;
        /// <summary>
        /// Update position of a single node based on forces already computed.
        /// </summary>
        /// <param name="n"></param>
        private void UpdatePosition(GraphNode n) {
            if (n.IsBeingDragged) return;
            var saved = n.Position;
            n.Position = (2 - NodeDamping) * n.Position -
                         (1 - NodeDamping) * n.PreviousPosition +
                         FixedDeltaTime * FixedDeltaTime * n.NetForce;
            n.PreviousPosition = saved;
        }

        /// <summary>
        /// Apply a spring force to two adjacent nodes to move them closer to targetEdgeLength.
        /// </summary>
        private void ApplySpringForce(int i, int j) {
            var td = this.topologicalDistance[i, j];
            var start = Nodes[i];
            var end = Nodes[j];
            var offset = start.Position - end.Position;
            var realDist = offset.Length();
            var force = Vector2.Zero;
            if (realDist > 0.001f) {
                if (td == short.MaxValue) {
                    if (realDist < 2 * TargetEdgeLength)
                        force = offset * (ComponentRepulsionGain / (realDist * realDist * realDist));
                } else
                {
                    var targetLength = TargetEdgeLength * td;
                    var lengthError = targetLength - realDist;
                    force = SpringStiffness * (lengthError / realDist) * offset;
                }
            }
            start.NetForce += force;
            end.NetForce -= force;
        }
        #endregion

        #region MouseInteraction

        public GraphNode? FindNode(Vector2 location, float nodeSize) =>
            Nodes.FirstOrDefault(n => Vector2.Distance(location, n.Position) <= nodeSize);

        #endregion

        public bool HasReverseEdge(GraphEdge e) => edgeCount.ContainsKey((e.End, e.Start));
    }
}
