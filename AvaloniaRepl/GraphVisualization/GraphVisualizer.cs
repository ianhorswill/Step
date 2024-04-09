using System;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using GraphViz;
using Step;
using Task = Step.Interpreter.Task;

namespace AvaloniaRepl.GraphVisualization
{
    public class GraphVisualizer : Control
    {
        public GraphVisualizer()
        {
        }

        private void MakeLayout()
        {
            var layoutBounds = new Rect(Bounds.Size);
            if (Graph != null)
            {
                Layout = new GraphLayout(Graph, layoutBounds);
                NodeLabels = Layout.Nodes.Select(n => new FormattedText(n.Label ?? n.ToString(),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, Typeface.Default, 14, n.Brush)).ToArray();
                Update();
            }
        }


        private Graph Graph => DataContext as Graph;
        private GraphLayout? Layout;

        private FormattedText[]? NodeLabels;
        private Pen redPen = new(new SolidColorBrush(Colors.Red), 3);
        private Pen greenPen = new(new SolidColorBrush(Colors.GreenYellow), 3);
        private Brush greenBrush = new SolidColorBrush(Colors.GreenYellow);

        public double NodeSize = 7;
        public Point TextOffset = new(0, -30);

        public float ArrowHeadSize = 10;

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            if (Layout == null)
                MakeLayout();
            if (Layout != null)
                Layout.Bounds = new Rect(e.NewSize);
        }

        private void Update()
        {
            Layout.FixedUpdate();
            InvalidateVisual();
            DispatcherTimer.RunOnce(Update, new TimeSpan(0, 0, 0, 0, 30));
        }

        public override void Render(DrawingContext context)
        {
            if (Layout == null)
                return;
            var b = Bounds;
            foreach (var n in Layout.Nodes)
            {
                var p = ToPoint(n.Position);
                context.DrawEllipse((n == Selected)?Brushes.GreenYellow:n.Brush, null, p, NodeSize, NodeSize);
                context.DrawText(NodeLabels[n.Index], p + TextOffset);
            }

            foreach (var e in Layout.Edges)
            {
                var pen = e.Start == Selected || e.End == Selected ? greenPen : e.Pen;
                context.DrawLine(pen, ToPoint(e.Start.Position), ToPoint(e.End.Position));
                if (e.IsDirected)
                {
                    var offset = e.End.Position - e.Start.Position;
                    var len = offset.Length();
                    if (len > 0.1)
                    {
                        var unit = offset / len;
                        var perp = 0.5f * new Vector2(unit.Y, -unit.X);
                        var end = ToPoint(e.End.Position-unit*(float)NodeSize);
                        context.DrawLine(pen, end, end+ToPoint(ArrowHeadSize * (perp - unit)));
                        context.DrawLine(pen, end, end+ToPoint(ArrowHeadSize * (-perp - unit)));
                    }
                }
            }
            Layout.FixedUpdate();
            base.Render(context);
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
                Selected = Layout.FindNode(position, (float)NodeSize);
            if (Selected != null && isDragging)
                Selected.SnapTo(position);
        }
    }
}
