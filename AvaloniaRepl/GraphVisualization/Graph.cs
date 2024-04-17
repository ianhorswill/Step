using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace GraphViz
{
    /// <summary>
    /// Represents a graph to be written to a .dot file for rendering using Graph
    /// This is the untyped base class.  Use the version with a type parameter to make a real graph.
    /// </summary>
    public abstract class Graph
    {
        internal static readonly Dictionary<string, object> EmptyAttributeDictionary = new Dictionary<string, object>();

        /// <summary>
        /// Attributes of the graph itself
        /// </summary>
        public readonly Dictionary<string, object> Attributes = new Dictionary<string, object>();
        /// <summary>
        /// Attributes to be applied by default to all nodes
        /// </summary>
        public readonly Dictionary<string, object> GlobalNodeAttributes = new Dictionary<string, object>();
        /// <summary>
        /// Attributes to be applied by default to all edges
        /// </summary>
        public readonly Dictionary<string, object> GlobalEdgeAttributes = new Dictionary<string, object>();

        internal int NextNodeUid;

        /// <summary>
        /// Write the value part of an attribute/value pair in .dot format
        /// </summary>
        internal static void WriteAttribute(object value, TextWriter o)
        {
            switch (value)
            {
                case string s:
                    WriteQuotedString(s, o);
                    break;

                default:
                    o.Write(value);
                    break;
            }
        }

        /// <summary>
        /// Write an attribute/value pair in .dot format
        /// </summary>
        /// <param name="attr">Name of the attribute</param>
        /// <param name="value">Value of the attribute</param>
        /// <param name="o">Stream to write to</param>
        internal static void WriteAttribute(string attr, object value, TextWriter o)
        {
            o.Write(attr);
            o.Write('=');
            WriteAttribute(value, o);
        }

        /// <summary>
        /// Write an attribute/value pair in .dot format
        /// </summary>
        /// <param name="attr">Name of the attribute + its value</param>
        /// <param name="o">Stream to write to</param>
        internal static void WriteAttribute(KeyValuePair<string, object> attr, TextWriter o)
            => WriteAttribute(attr.Key, attr.Value, o);

        /// <summary>
        /// Write a series of attribute/value pairs in .dot format
        /// </summary>
        /// <param name="attributes">List of attribute/value pairs</param>
        /// <param name="preamble">string to print before a pair</param>
        /// <param name="postamble">String to print after a pair</param>
        /// <param name="o">Stream to print to</param>
        internal static void WriteAttributeList(IEnumerable<KeyValuePair<string, object>> attributes,
            string? preamble, string? postamble,
            TextWriter o)
        {
            foreach (var p in attributes)
            {
                o.Write(preamble??"");
                WriteAttribute(p, o);
                o.Write(postamble??"");
            }
        }

        internal static void WriteQuotedString(string s, TextWriter o)
        {
            o.Write('"');
            foreach (var c in s)
            {
                if (c == '"' || c == '\\')
                    o.Write('\\');
                o.Write(c);
            }

            o.Write('"');
        }

        public abstract IEnumerable<(object Node, Dictionary<string, object> Attributes, string Label)> NodesUntyped { get; }
        public abstract IEnumerable<(object From, object To, Dictionary<string, object>? Attributes, bool IsDirected, string? Label)> EdgesUntyped { get; }

        public abstract float UndirectedDistance(object from, object to);
        public abstract float Diameter { get; }

        public abstract int ConnectedComponentCount { get; }
        public abstract int ConnectedComponentNumber(object node);

        /// <summary>
        /// Change colors in graph so that each connected component has its own color
        /// </summary>
        public abstract void RecolorByComponent();
    }

    /// <summary>
    /// A graph to be written to a .dot file for visualization using Graph
    /// </summary>
    /// <typeparam name="T">The data type of the nodes in the graph</typeparam>
    public class Graph<T> : Graph
    {
        /// <summary>
        /// Optional function to compute attributes of a node
        /// </summary>
        public Func<T, IEnumerable<KeyValuePair<string, object>>>? DefaultNodeAttributes;
        /// <summary>
        /// Optional function to compute attributes of an edge
        /// </summary>
        public Func<Edge, IDictionary<string, object>>? DefaultEdgeAttributes;

        /// <summary>
        /// Function to compute the ID string to use for a node in the file, as distinct from its label
        /// By default, this just assigns a serial number to each node.  But you can specify your own function.
        /// </summary>
        public Func<T, string> NodeId;

        /// <summary>
        /// Function to compute the label to display inside a node
        /// </summary>
        public Func<T, string> NodeLabel;

        /// <summary>
        /// Function to compute the cluster to assign a node to, if any.
        /// </summary>
        public Func<T, Cluster?>? NodeCluster; 
        
        /// <summary>
        /// The internal ID string assigned to a node
        /// This is not defined until the node is added to the graph
        /// </summary>
        public readonly Dictionary<T, string> IdOf;

        /// <summary>
        /// The set of all nodes in the graph
        /// </summary>
        private readonly HashSet<T> nodes;

        public readonly Dictionary<T, int> NodeIndex;

        /// <summary>
        /// The set of all nodes in the graph
        /// </summary>
        public ISet<T> Nodes => nodes;

        /// <summary>
        /// The attributes assigned to a given node
        /// </summary>
        public Dictionary<T, Dictionary<string, object>>
            NodeAttributes;

        public override IEnumerable<(object Node, Dictionary<string, object> Attributes, string Label)> NodesUntyped =>
            nodes.Select(n => ((object)n!, NodeAttributes[n], NodeLabel(n)));

        public override
            IEnumerable<(object From, object To, Dictionary<string, object>? Attributes, bool IsDirected, string? Label)> EdgesUntyped =>
            edges.Select(e => ((object)CanonicalizeNode(e.StartNode)!, (object)CanonicalizeNode(e.EndNode)!, e.Attributes, e.Directed, e.Label));

        /// <summary>
        /// Make a graph to be rendered using Graph.
        /// </summary>
        public Graph(IEqualityComparer<T>? comparer = null)
        {
            NodeId = (_) => $"v{NextNodeUid++}";
            NodeLabel = v => (v == null)?"null":v.ToString()!;
            DefaultNodeAttributes = n => EmptyAttributeDictionary;
            DefaultEdgeAttributes = edge => EmptyAttributeDictionary;
            IdOf = new Dictionary<T, string>(comparer);
            NodeIndex = new Dictionary<T, int>(comparer);
            nodes = new HashSet<T>(comparer);
            NodeAttributes = new Dictionary<T, Dictionary<string, object>>(comparer);
        }

        /// <summary>
        /// Add a node to the graph.
        /// IF the node is already present in the graph, this does nothing.
        /// </summary>
        public void AddNode(T n)
        {
            if (nodes.Contains(n))
                return;
            NodeIndex[n] = nodes.Count;
            nodes.Add(n);
            NodeAttributes[n] = new Dictionary<string, object>();
            if (DefaultNodeAttributes != null)
                foreach (var p in DefaultNodeAttributes(n))
                    NodeAttributes[n][p.Key] = p.Value;
            IdOf[n] = NodeId(n);
        }
        
        /// <summary>
        /// Add the edge.
        /// If edge is already present, merge the attributes of the edge with the attributes listed here
        /// </summary>
        public void AddEdge(Edge e, bool addNodes = false)
        {
            e.StartNode = CanonicalizeNode(e.StartNode);
            e.EndNode = CanonicalizeNode(e.EndNode);

            if (edges.TryGetValue(e, out var canonical))
                canonical.AddAttributes(e);
            else
            {
                if (DefaultEdgeAttributes != null)
                    e.AddAttributes(DefaultEdgeAttributes(e));
                edges.Add(e);
                // This breaks AddReachableFrom
                if (addNodes)
                {
                    AddNode(e.StartNode);
                    AddNode(e.EndNode);
                }
            }
        }

        private T CanonicalizeNode(T node) => nodes.TryGetValue(node, out var actual) ? actual : node;

        /// <summary>
        /// Add all the nodes listed, and all the nodes reachable from them via the nodeEdges.
        /// The edges are added too.
        /// In other words, this adds the connected components of all the nodes in roots.
        /// </summary>
        /// <param name="roots">All the nodes to start from</param>
        /// <param name="nodeEdges">Function to list the set of edges incident on a specified node</param>
        public void AddReachableFrom(IEnumerable<T> roots, Func<T, IEnumerable<Edge>> nodeEdges)
        {
            foreach (var root in roots)
                AddReachableFrom(root, nodeEdges);
        }

        /// <summary>
        /// Add this node and all the nodes reachable from it via nodeEdges.  The edges are added too.
        /// In other words, this adds the specified node's connected component.
        /// </summary>
        /// <param name="root">Node to start tracing from</param>
        /// <param name="nodeEdges">Function to list the set of edges incident on a specified node</param>
        public void AddReachableFrom(T root, Func<T, IEnumerable<Edge>> nodeEdges)
        {
            if (nodes.Contains(root))
                return;
            AddNode(root);
            foreach (var e in nodeEdges(root))
            {
                AddEdge(e);
                AddReachableFrom(e.StartNode,nodeEdges);
                AddReachableFrom(e.EndNode,nodeEdges);
            }
        }


        #region Output formatting
        /// <summary>
        /// Write the graph to the specified stream
        /// </summary>
        /// <param name="o">Stream to write to</param>
        public void WriteGraph(TextWriter o)
        {
            FinalizeGraphClusters();
            var writtenNodes = new HashSet<T>();
            void WriteCluster(Cluster c)
            {
                o.Write("subgraph ");
                WriteQuotedString("cluster_" + c.Name, o);
                o.WriteLine("{");

                WriteAttributeList(c.Attributes, "", "\n", o);

                foreach (var n in c.Nodes)
                    WriteNode(n, o);

                foreach (var s in c.SubClusters)
                    WriteCluster(s);

                o.WriteLine('}');
                writtenNodes.UnionWith(c.Nodes);
            }

            o.WriteLine("digraph {");
            o.WriteLine("splines=true");

            WriteAttributeList(Attributes, "", "\n", o);

            if (GlobalNodeAttributes.Count > 0)
            {
                o.Write("node [");
                WriteAttributeList(GlobalNodeAttributes, " ", "", o);
                o.WriteLine("]");
            }

            if (GlobalEdgeAttributes.Count > 0)
            {
                o.Write("edge [");
                WriteAttributeList(GlobalEdgeAttributes, " ", "", o);
                o.WriteLine("]");
            }


            foreach (var c in topLevelClusters)
            {
                WriteCluster(c);
            }
            foreach (var v in nodes) 
                if (!writtenNodes.Contains(v))
                    WriteNode(v, o);
            foreach (var e in edges) WriteEdge(e, o);
            o.WriteLine("}");
        }

        private bool finalized;
        private void FinalizeGraphClusters()
        {
            if (finalized)
                return;
            foreach (var n in nodes)
            {
                var c = NodeCluster?.Invoke(n);
                c?.Nodes.Add(n);
            }

            finalized = true;
        }

        /// <summary>
        /// Write the graph in .dot format to the specified file.
        /// </summary>
        /// <param name="path">Path to the file to write</param>
        public void WriteGraph(string path)
        {
            using var file = File.CreateText(path);
            WriteGraph(file);
        }

        /// <summary>
        /// Write an edge in DOT format
        /// </summary>
        private void WriteEdge(Edge edge, TextWriter o)
        {
            o.Write($"{IdOf[edge.StartNode]} -> {IdOf[edge.EndNode]}");
            if ((edge.Attributes != null && edge.Attributes.Count > 0) || !edge.Directed)
            {
                o.Write(" [");
                if (!edge.Directed)
                    o.Write("dir=none");
                if (edge.Attributes != null)
                    foreach (var pair in edge.Attributes)
                    {
                        o.Write(' ');
                        WriteAttribute(pair, o);
                    }
                o.Write(" ]");
            }
            o.WriteLine();
        }

        /// <summary>
        /// Write a node in DOT format
        /// </summary>
        private void WriteNode(T n, TextWriter o)
        {
            o.Write(IdOf[n]);
            o.Write(" [");
            o.Write(" label = ");
            WriteAttribute(NodeLabel(n), o);
            WriteAttributeList(NodeAttributes[n], " ", null, o);
            o.WriteLine("];");
        }
        #endregion
        
        #region Edges
        /// <summary>
        /// The set of all edges in the graph
        /// </summary>
        public ISet<Edge> Edges => edges;

        private readonly HashSet<Edge> edges = new HashSet<Edge>();

        /// <summary>
        /// Represents an edge in a Graph graph
        /// </summary>
        public class Edge
        {
            /// <summary>
            /// Node this edge originates at.
            /// If this is an undirected edge, then StartNode and EndNode are interchangeable
            /// </summary>
            public T StartNode;
            /// <summary>
            /// Node this edge ends at.
            /// If this is an undirected edge, then StartNode and EndNode are interchangeable
            /// </summary>
            public T EndNode;
            /// <summary>
            /// Whether this is a directed edge or not
            /// </summary>
            public readonly bool Directed;
            /// <summary>
            /// Text to mark this edge with, or null, if edge is to be unlabeled.
            /// </summary>
            public readonly string? Label;
            /// <summary>
            /// Attributes to be applied to this edge
            /// </summary>
            public Dictionary<string, object>? Attributes;

            /// <summary>
            /// Make an edge
            /// </summary>
            /// <param name="startNode">Node the edge should start from.</param>
            /// <param name="endNode">Node the edge should end at</param>
            /// <param name="directed">Whether the edge is directed.  If not, startNode and endNode can be switched.</param>
            /// <param name="label">Text to display along with the edge, or null</param>
            /// <param name="attributes">Attributes to apply to the edge, if any</param>
            public Edge(T startNode, T endNode, bool directed = true, string? label = null, Dictionary<string,object>? attributes = null)
            {
                StartNode = startNode;
                this.EndNode = endNode;
                Directed = directed;
                Label = label;
                Attributes = attributes;
            }

            /// <summary>
            /// Copy the attributes from the specified edge into this edge's attributes
            /// </summary>
            /// <param name="e"></param>
            public void AddAttributes(Edge e) => AddAttributes(e.Attributes);

            /// <summary>
            /// Add the specified attributes to this edge
            /// </summary>
            /// <param name="attributes"></param>
            public void AddAttributes(IEnumerable<KeyValuePair<string, object>>? attributes)
            {
                if (attributes == null) return;
                Attributes ??= new Dictionary<string, object>();
                foreach (var pair in attributes) Attributes[pair.Key] = pair.Value;
            }

            /// <summary>
            /// True if two edges are the same.
            /// Edges are the same if they have the same start and end nodes, directedness, and label.
            /// If the node isn't directed, the the order of start and end nodes doesn't matter.
            /// Attributes are ignored for purposes of equality.
            /// </summary>
            public static bool operator ==(Edge a, Edge b)
            {
                if (a.Directed != b.Directed || !Equals(a.Label, b.Label))
                    return false;
                if (a.Directed)
                    return EqualityComparer<T>.Default.Equals(a.StartNode, b.StartNode)
                           && EqualityComparer<T>.Default.Equals(a.EndNode, b.EndNode);
                return (EqualityComparer<T>.Default.Equals(a.StartNode, b.StartNode)
                        && EqualityComparer<T>.Default.Equals(a.EndNode, b.EndNode))
                       || (EqualityComparer<T>.Default.Equals(a.StartNode, b.EndNode)
                           && EqualityComparer<T>.Default.Equals(a.EndNode, b.StartNode));
            }

            /// <summary>
            /// True if the nodes are not the same
            /// </summary>
            public static bool operator !=(Edge a, Edge b) => !(a == b);

            /// <summary>
            /// Make an edge from a tuple
            /// </summary>
            public static implicit operator Edge((T start, T end) e) => new Edge(e.Item1, e.Item2);
            /// <summary>
            /// Make an edge from a tuple
            /// </summary>
            public static implicit operator Edge((T start, T end, string label) e) => new Edge(e.Item1, e.Item2, true, e.Item3);
            /// <summary>
            /// Make an edge from a tuple
            /// </summary>
            public static implicit operator Edge((T start, T end, string label, bool directed) e) => new Edge(e.Item1, e.Item2, e.Item4, e.Item3);

            /// <summary>
            /// True if obj is an edge that is == to this edge
            /// Edges are the same if they have the same start and end nodes, directedness, and label.
            /// If the node isn't directed, the the order of start and end nodes doesn't matter.
            /// Attributes are ignored for purposes of equality.
            /// </summary>
            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj is Edge e && this == e;
            }

            /// <summary>
            /// Return a hash code for the edge.  If the edge is directed, then it is symmetric in the start and end nodes
            /// </summary>
            public override int GetHashCode()
            {
                // We need the hash to be symmetric in InNode and OutNode in case the edge is undirected.
                return HashCode.Combine(StartNode!.GetHashCode() + EndNode!.GetHashCode(), Directed, Label);
            }
        }
        #endregion

        #region Clusters
        /// <summary>
        /// A cluster of nodes to be grouped together during rendering
        /// </summary>
        public class Cluster
        {
            /// <summary>
            /// Name of the cluster
            /// </summary>
            public string Name;
            /// <summary>
            /// Nodes in the cluster
            /// </summary>
            public HashSet<T> Nodes = new HashSet<T>();
            /// <summary>
            /// Attributes rendering for this cluster
            /// </summary>
            public Dictionary<string, object> Attributes = new Dictionary<string, object>();

            /// <summary>
            /// Other clusters to be rendered inside this cluster
            /// </summary>
            public List<Cluster> SubClusters = new List<Cluster>();

            internal Cluster(string name, Cluster? parent = null)
            {
                Name = name;
                parent?.SubClusters.Add(this);
            }
        }

        /// <summary>
        /// Clusters for this graph that are not inside other clusters
        /// </summary>
        private readonly List<Cluster> topLevelClusters = new List<Cluster>();

        /// <summary>
        /// Add a cluster to the graph
        /// </summary>
        /// <param name="name">Name to give to the cluster</param>
        /// <param name="parent">Cluster inside of which to render this cluster, or null</param>
        /// <returns></returns>
        public Cluster MakeCluster(string name, Cluster? parent = null)
        {
            var c = new Cluster(name, parent);
            if (parent == null)
                topLevelClusters.Add(c);
            return c;
        }
        #endregion

        #region Topology
        // ReSharper disable once InconsistentNaming
        private float[,]? _nodeDistances;

        /// <summary>
        /// UndirectedDistance between nodes, ignoring edge direction, or infinity, if the nodes are disconnected
        /// </summary>
        private float[,] NodeDistances 
        {
            get
            {
                if (_nodeDistances == null)
                {
                    FinalizeGraphClusters();
                    var count = nodes.Count;

                    var distances = new float[count, count];
                    for (var i = 0; i < count; i++)
                    for (var j = 0; j < count; j++)
                        distances[i, j] = float.PositiveInfinity;

                    for (var i = 0; i < count; i++)
                        distances[i, i] = 0;

                    foreach (var edge in edges)
                    {
                        var s = NodeIndex[edge.StartNode];
                        var e = NodeIndex[edge.EndNode];
                        distances[e, s] = distances[s, e] = 1;
                    }

                    for (var k = 0; k < count; k++)
                    for (var i = 0; i < count; i++)
                    for (var j = 0; j < count; j++)
                        distances[j,i] = distances[i, j] = Math.Min(distances[i, j], distances[i, k] + distances[k, j]);
                    _nodeDistances = distances;

                    //for (var i = 0; i < count; i++)
                    //for (var j = 0; j < count; j++)
                    //    Debug.Assert(distances[i,j] == distances[j,i]);
                }

                return _nodeDistances;
            }
        }

        /// <summary>
        /// UndirectedDistance between nodes, ignoring edge direction, or infinity, if the nodes are disconnected
        /// </summary>
        public override float UndirectedDistance(object node1, object node2) =>
            NodeDistances[NodeIndex[(T)node1], NodeIndex[(T)node2]];

        // ReSharper disable once InconsistentNaming
        private float _diameter = -1;

        /// <summary>
        /// UndirectedDistance between the two most distant nodes that are still connected.
        /// </summary>
        public override float Diameter
        {
            get
            {
                if (_diameter < 0)
                {
                    var dist = NodeDistances;
                    var count = nodes.Count;
                    float diameter = dist[0, 0];
                    for (var i = 0; i < count; i++)
                    for (var j = 0; j < count; j++)
                    {
                        var d = dist[i, j];
                        if (!float.IsPositiveInfinity(d) && d > diameter)
                            diameter = d;
                    }
                    _diameter = diameter;
                }

                return _diameter;
            }
        }

        private int _connectedComponentCount;
        private Dictionary<T, int>? _connectedComponent;
        private int[] _nodeComponentNumbers;

        private void FindConnectedComponents()
        {
            _nodeComponentNumbers = new int[nodes.Count];
            Array.Fill(_nodeComponentNumbers, -1);
            for (int i = 0; i < nodes.Count; i++)
            {
                if (_nodeComponentNumbers[i] < 0)
                {
                    // New component
                    for (int j = 0; j < nodes.Count; j++)
                        if (!float.IsPositiveInfinity(NodeDistances[i, j]))
                        {
                            // They're connected
                            _nodeComponentNumbers[j] = _connectedComponentCount;
                        }
                    _connectedComponentCount++;
                }
            }
            Debug.Assert(_nodeComponentNumbers.All(n => n>=0));
        }

        public override int ConnectedComponentCount
        {
            get
            {
                if (_nodeComponentNumbers == null)
                    FindConnectedComponents();
                return _connectedComponentCount;
            }
        }

        public int ConnectedComponentNumber(T node)
        {
            if (_nodeComponentNumbers == null)
                FindConnectedComponents();
            return _nodeComponentNumbers[NodeIndex[node]];
        }

        public override int ConnectedComponentNumber(object node) => ConnectedComponentNumber((T)node);
        #endregion

        public override void RecolorByComponent()
        {
            var palette = new string[ConnectedComponentCount];
            if (ConnectedComponentCount == 1)
                palette[0] = "#00FF00";
            else
                for (var i = 0; i < ConnectedComponentCount; i++)
                {
                    var green = (int)((255.0  * i)/(ConnectedComponentCount-1));
                    palette[i] = $"#00{green:X2}{255 - green:X2}";
                }

            foreach (var n in NodeIndex)
                NodeAttributes[n.Key]["fillcolor"] = palette[_nodeComponentNumbers[n.Value]];

            foreach (var e in edges)
            {
                var component = _nodeComponentNumbers[NodeIndex[e.StartNode]];
                e.Attributes ??= new Dictionary<string, object>();
                e.Attributes["color"] = palette[component];
            }
        }
    }
}
