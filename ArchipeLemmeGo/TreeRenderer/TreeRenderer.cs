using ArchipeLemmeGo.Bot;
using SkiaSharp;
using System.Collections.Generic;

public static class TreeRenderer
{
    // --- Colors (requested) ---
    private static readonly SKColor Background = SKColor.Parse("#2c2d32");
    private static readonly SKColor TextColor = SKColor.Parse("#e9e9ea");
    private static readonly SKColor EdgeColor = SKColor.Parse("#323339");

    // --- Label background ---
    private const float LabelPadding = 2f;
    private static readonly SKColor LabelBackground = SKColor.Parse("#1e1f24");

    // --- Public enums ---
    public enum NodeShape { Circle, Rectangle, Hexagon }
    public enum NodePalette { Green, Yellow, Grey, Blue, Red }

    // --- Public data types + API ---
    public sealed class TreeNode
    {
        public string Id { get; }
        public string Label { get; set; }
        public string? SubLabel { get; set; }
        public NodeShape Shape { get; set; } = NodeShape.Circle;
        public NodePalette Palette { get; set; } = NodePalette.Grey;
        public readonly List<TreeNode> Children = new();

        public TreeNode(string id, string label, NodeShape shape = NodeShape.Circle, NodePalette palette = NodePalette.Grey)
        {
            Id = id;
            Label = label;
            Shape = shape;
            Palette = palette;
        }

        public TreeNode AddChild(string id, string label, NodeShape shape = NodeShape.Circle, NodePalette palette = NodePalette.Grey)
        {
            var child = new TreeNode(id, label, shape, palette);
            Children.Add(child);
            return child;
        }
    }

    public sealed class Tree
    {
        public TreeNode Root { get; }
        public Tree(TreeNode root) => Root = root;
    }

    // --- Public render entrypoint ---
    public static void Render(Tree tree, int width, int height, string path, float nodeRadius = 40f, float levelHeight = 110f, float nodeWidthPerLeaf = 80f)
    {
        using var bmp = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bmp);
        canvas.Clear(Background);

        // 1) Layout
        var positions = LayoutTree(tree.Root, width, height, nodeRadius, levelHeight, nodeWidthPerLeaf);

        // 2) Draw edges
        using var edgePaint = new SKPaint
        {
            Color = EdgeColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 8f,
            IsAntialias = true
        };
        DrawEdges(canvas, tree.Root, positions, edgePaint);

        // 3) Draw nodes + labels
        using var textPaint = new SKPaint
        {
            Color = TextColor,
            IsAntialias = true,
            TextSize = 16f,
            TextAlign = SKTextAlign.Center
        };

        using var subTextPaint = new SKPaint
        {
            Color = SKColor.Parse("#b0b0b1"),
            IsAntialias = true,
            TextSize = 13f,
            TextAlign = SKTextAlign.Center
        };

        foreach (var (node, pos) in positions)
        {
            var (fill, stroke) = ResolvePalette(node.Palette);
            using var fillPaint = new SKPaint { Color = fill, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var strokePaint = new SKPaint { Color = stroke, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, IsAntialias = true };

            DrawShape(canvas, node.Shape, pos, nodeRadius, fillPaint, strokePaint);

            // draw main label
            DrawTextWithBackground(canvas, node.Label, pos, textPaint);

            // draw sublabel
            if (!string.IsNullOrEmpty(node.SubLabel))
            {
                var fm2 = subTextPaint.FontMetrics;
                var offsetY = (fm2.Descent - fm2.Ascent) + 4f;
                var subPos = new SKPoint(pos.X, pos.Y + offsetY);
                DrawTextWithBackground(canvas, node.SubLabel, subPos, subTextPaint);
            }
        }

        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        using var fs = File.Open(path, FileMode.Create, FileAccess.Write);
        data.SaveTo(fs);
    }

    // --- Text with background ---
    private static void DrawTextWithBackground(SKCanvas canvas, string text, SKPoint center, SKPaint paint)
    {
        float textWidth = paint.MeasureText(text);
        var fm = paint.FontMetrics;
        float textHeight = fm.Descent - fm.Ascent;

        var rect = SKRect.Create(
            center.X - textWidth / 2f - LabelPadding,
            center.Y + fm.Ascent + 6,
            textWidth + LabelPadding * 2f,
            textHeight + LabelPadding * 2f
        );

        using var bgPaint = new SKPaint { Color = LabelBackground, Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawRoundRect(rect, 4f, 4f, bgPaint);
        canvas.DrawText(text, center.X, center.Y - (fm.Ascent + fm.Descent) / 2f, paint);
    }

    // --- Internal: palette mapping ---
    private static (SKColor fill, SKColor stroke) ResolvePalette(NodePalette p) => p switch
    {
        NodePalette.Green => (SKColor.Parse("#10B981"), SKColor.Parse("#089168")),
        NodePalette.Yellow => (SKColor.Parse("#F59E0B"), SKColor.Parse("#B45309")),
        NodePalette.Grey => (SKColor.Parse("#6B7280"), SKColor.Parse("#4B5563")),
        NodePalette.Blue => (SKColor.Parse("#2563EB"), SKColor.Parse("#1E40AF")),
        NodePalette.Red => (SKColor.Parse("#EF4444"), SKColor.Parse("#B91C1C")),
        _ => (SKColor.Parse("#6B7280"), SKColor.Parse("#4B5563"))
    };

    // --- Internal: draw shapes ---
    private static void DrawShape(SKCanvas canvas, NodeShape shape, SKPoint center, float r, SKPaint fill, SKPaint stroke)
    {
        switch (shape)
        {
            case NodeShape.Circle:
                canvas.DrawCircle(center, r, fill);
                canvas.DrawCircle(center, r, stroke);
                break;

            case NodeShape.Rectangle:
                {
                    var w = r * 2.2f;
                    var h = r * 1.6f;
                    var rect = SKRect.Create(center.X - w / 2f, center.Y - h / 2f, w, h);
                    var rr = MathF.Min(r * 0.4f, 10f);
                    canvas.DrawRoundRect(rect, rr, rr, fill);
                    canvas.DrawRoundRect(rect, rr, rr, stroke);
                    break;
                }

            case NodeShape.Hexagon:
                {
                    using var path = MakeRegularPolygonPath(center, r, sides: 6, rotationDeg: 30f);
                    canvas.DrawPath(path, fill);
                    canvas.DrawPath(path, stroke);
                    break;
                }
        }
    }

    private static SKPath MakeRegularPolygonPath(SKPoint c, float radius, int sides, float rotationDeg = 0f)
    {
        var path = new SKPath();
        var rot = rotationDeg * MathF.PI / 180f;
        for (int i = 0; i < sides; i++)
        {
            var angle = rot + (2f * MathF.PI * i / sides);
            var x = c.X + radius * MathF.Cos(angle);
            var y = c.Y + radius * MathF.Sin(angle);
            if (i == 0) path.MoveTo(x, y);
            else path.LineTo(x, y);
        }
        path.Close();
        return path;
    }

    // --- Internal: edges ---
    private static void DrawEdges(SKCanvas canvas, TreeNode node, Dictionary<TreeNode, SKPoint> pos, SKPaint paint)
    {
        var a = pos[node];
        foreach (var child in node.Children)
        {
            var b = pos[child];
            canvas.DrawLine(a, b, paint);
            DrawEdges(canvas, child, pos, paint);
        }
    }

    // --- Internal: layout ---
    private static Dictionary<TreeNode, SKPoint> LayoutTree(TreeNode root, int width, int height, float r, float levelHeight, float nodeWidthPerLeaf)
    {
        var leafCounts = new Dictionary<TreeNode, int>();
        ComputeLeafCounts(root, leafCounts);

        var totalLeaves = leafCounts[root];
        var margin = MathF.Max(24f, r * 2f);
        var unit = nodeWidthPerLeaf;

        var positions = new Dictionary<TreeNode, SKPoint>();
        float nextLeafCenter = margin + unit / 2f;

        PlaceNode(root, depth: 0);
        return positions;

        void PlaceNode(TreeNode n, int depth)
        {
            if (n.Children.Count == 0)
            {
                positions[n] = new SKPoint(nextLeafCenter, margin + depth * levelHeight + r);
                nextLeafCenter += unit;
                return;
            }

            foreach (var c in n.Children) PlaceNode(c, depth + 1);

            var first = positions[n.Children[0]];
            var last = positions[n.Children[^1]];
            var parentX = (first.X + last.X) / 2f;
            var parentY = margin + depth * levelHeight + r;
            positions[n] = new SKPoint(parentX, parentY);
        }
    }

    private static int ComputeLeafCounts(TreeNode n, Dictionary<TreeNode, int> map)
    {
        if (n.Children.Count == 0) { map[n] = 1; return 1; }
        int s = 0;
        foreach (var c in n.Children) s += ComputeLeafCounts(c, map);
        map[n] = s;
        return s;
    }

    // --- Auto-sizing wrapper ---
    public static void RenderAuto(Tree tree, string path, float nodeRadius = 100f, float levelHeight = 300f, float nodeWidthPerLeaf = 350f, float margin = 40f)
    {
        var leafCounts = new Dictionary<TreeNode, int>();
        int leaves = ComputeLeafCounts(tree.Root, leafCounts);
        int depth = ComputeDepth(tree.Root);

        int width = (int)((leaves + 1) * nodeWidthPerLeaf + margin * 2);
        int height = (int)((depth + 1) * levelHeight + margin * 2);

        Render(tree, width, height, path, nodeRadius, levelHeight, nodeWidthPerLeaf);
    }

    private static int ComputeDepth(TreeNode node)
    {
        if (node.Children.Count == 0) return 1;
        return 1 + node.Children.Max(ComputeDepth);
    }

    // --- Dependency tree adapters ---
    public static TreeNode FromDependancyNode(DepTreeNode node)
    {
        var outNode = new TreeNode(
            id: Guid.NewGuid().ToString(),
            label: node.Text,
            shape: node is DepTreeItemNode ? NodeShape.Circle : node is DepTreeLocationNode ? NodeShape.Rectangle : NodeShape.Hexagon,
            palette: node.Status switch
            {
                DepTreeNodeStatus.CanDo => NodePalette.Blue,
                DepTreeNodeStatus.Blocked => NodePalette.Yellow,
                DepTreeNodeStatus.Done => NodePalette.Green,
                DepTreeNodeStatus.Unreachable => NodePalette.Red,
                _ => NodePalette.Grey
            });
        outNode.SubLabel = node.SubText;
        outNode.Children.AddRange(node.Children.Select(FromDependancyNode));
        return outNode;
    }

    public static Tree FromDependancyTree(DependancyTree tree)
    {
        var root = FromDependancyNode(tree.RootNode);
        return new Tree(root);
    }

    // --- Example ---
    public static Tree MakeSample()
    {
        var root = new TreeNode("root", "Root", NodeShape.Hexagon, NodePalette.Blue);
        var c1 = root.AddChild("c1", "Child 1 bananaboom", NodeShape.Circle, NodePalette.Green);
        var c2 = root.AddChild("c2", "Child 2", NodeShape.Rectangle, NodePalette.Yellow);
        c1.AddChild("c1a", "Leaf A", NodeShape.Rectangle, NodePalette.Grey);
        c1.AddChild("c1b", "Leaf B", NodeShape.Circle, NodePalette.Blue);
        c2.AddChild("c2a", "Leaf C", NodeShape.Hexagon, NodePalette.Green);
        return new Tree(root);
    }
}
