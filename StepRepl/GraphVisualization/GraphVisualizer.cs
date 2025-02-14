﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using GraphViz;

namespace StepRepl.GraphVisualization
{
    public class GraphVisualizer : Control
    {
        private void MakeLayout()
        {
            var layoutBounds = new Rect(Bounds.Size);
            if (Graph != null)
            {
                layout = new GraphLayout(Graph, layoutBounds);
                nodeLabels = layout.Nodes.Select(n => new FormattedText(n.Label ?? n.ToString()!,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, Typeface.Default, 14, n.Brush)).ToArray();
                foreach (var e in layout.Edges)
                    if (e.Label != null)
                    {
                        edgeLabels[e] = new FormattedText(e.Label, CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight, Typeface.Default, 14, e.Pen.Brush);
                    }

                Update();
            }
        }


        private Graph? Graph => DataContext as Graph;
        private GraphLayout? layout;

        private FormattedText[]? nodeLabels;
        private readonly Dictionary<object, FormattedText?> edgeLabels = new Dictionary<object, FormattedText?>();
        //private readonly Pen redPen = new(new SolidColorBrush(Colors.Red), 3);
        private readonly Pen greenPen = new(new SolidColorBrush(Colors.GreenYellow), 3);
        //private readonly Brush greenBrush = new SolidColorBrush(Colors.GreenYellow);

        public double NodeSize = 7;
        public Point TextOffset = new(0, -30);

        public float ArrowHeadSize = 10;

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            if (layout == null)
                MakeLayout();
            if (layout != null)
                layout.Bounds = new Rect(e.NewSize);
        }

        private void Update()
        {
            layout!.FixedUpdate();
            //InvalidateVisual();
            //DispatcherTimer.RunOnce(Update, new TimeSpan(0, 0, 0, 0, 30));
            DispatcherTimer.RunOnce(InvalidateVisual, new TimeSpan(0, 0, 0, 0, 30));
        }

        public override void Render(DrawingContext context)
        {
            if (layout == null)
                return;

            foreach (var e in layout.Edges)
            {
                var pen = e.Start == Selected || e.End == Selected ? greenPen : e.Pen;
                var startEndOffset = e.End.Position - e.Start.Position;
                var len = startEndOffset.Length();
                if (len < 1)
                    continue;
                // Unit vector in the direction of the line
                var unit = startEndOffset / len;
                // Unit vector perpendicular to the line
                var perp = new Vector2(unit.Y, -unit.X);
                // Distance the drawing of the edge should be offset from a direct line connecting the nodes
                var perpOffset = EdgeOffset(e) * perp;
                // Offset from the center of the start node for drawing the edge
                var tangentialOffset = unit * (float)NodeSize;
                // Tip of the arrow
                var end = ToPoint(e.End.Position + perpOffset - tangentialOffset);
                // Draw primary line
                context.DrawLine(pen, ToPoint(e.Start.Position + perpOffset + tangentialOffset), end);

                if (e.IsDirected && len > 1)
                {
                    // Draw arrowhead
                    context.DrawLine(pen, end, end + ToPoint(ArrowHeadSize * (0.5f * perp - unit)));
                    context.DrawLine(pen, end, end + ToPoint(ArrowHeadSize * (-0.5f * perp - unit)));
                }
                if (e.Label != null)
                {
                    var edgeLabel = edgeLabels[e];
                    var textPosition = ToPoint(perpOffset + (float)edgeLabel!.Height * perp + 0.5f * (e.Start.Position + e.End.Position - unit * (float)edgeLabel.Width));
                    var t = context.PushTransform(Matrix.CreateRotation(Math.Atan2(unit.Y, unit.X)) * Matrix.CreateTranslation(textPosition));
                    context.DrawText(edgeLabel, new Point(0, 0));
                    t.Dispose();
                }
            }

            foreach (var n in layout.Nodes)
            {
                var p = ToPoint(n.Position);
                if (n == Selected)
                    continue;
                context.DrawEllipse(n.Brush, null, p, NodeSize, NodeSize);
                var nodeLabel = nodeLabels![n.Index];
                context.DrawText(nodeLabel, p + TextOffset - new Point(0.5 * nodeLabel.Width, 0));
            }

            if (Selected != null)
            {
                var p = ToPoint(Selected.Position);
                context.DrawEllipse(Brushes.Yellow, null, p, NodeSize, NodeSize);
                //var nodeLabel = nodeLabels![Selected.Index];
                var backgroundText = new FormattedText(Selected.Label??"", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 24, Brushes.Black);
                var center = p + 1.2*TextOffset - new Point(0.5 * backgroundText.Width, 0);
                for (var i = -2; i < 3; i++)
                for (var j = -2; j < 3; j++)
                    context.DrawText(backgroundText, center+new Point(i,j));
                var foregroundText = new FormattedText(Selected.Label??"", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 24, Brushes.Yellow);
                context.DrawText(foregroundText, center);
            }

            base.Render(context);
            Update();
        }

        private const float EdgeSpacing = 16;

        private float EdgeOffset(GraphLayout.GraphEdge e)
        {
            var initialSpacing = layout!.HasReverseEdge(e) ? -0.75f: -1;
            return EdgeSpacing*(initialSpacing+e.RenderPosition);
        }

        Point ToPoint(Vector2 v) => new Point(v.X, v.Y);
        Vector2 ToVector2(Point p) => new Vector2((float)p.X, (float)p.Y);

        public GraphLayout.GraphNode? Selected;

        private bool isDragging;

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            isDragging = true;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            isDragging = false;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            var position = ToVector2(e.GetPosition(this));
            if (!isDragging)
                Selected = layout!.FindNode(position, (float)NodeSize);
            if (Selected != null && isDragging)
                Selected.SnapTo(position);
        }
    }
}
