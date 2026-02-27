using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SqlAnalyzer.Domain.Model;

namespace SqlAnalyzer.App.Services;

public sealed class DiagramService
{
    public DiagramArtifacts Generate(SqlStatement statement)
    {
        DebugLog.Write($"Generate start: statement={statement.StatementType}, tables={statement.Tables.Count}, relations={statement.Relations.Count}");
        string mermaidText = BuildMermaidText(statement.Tables, statement.Relations);

        try
        {
            byte[] pngBytes = RenderPng(statement.Tables, statement.Relations);
            DebugLog.Write($"Generate success: pngBytes={pngBytes.Length}, mermaidLength={mermaidText.Length}");
            return new DiagramArtifacts
            {
                MermaidText = mermaidText,
                PngBytes = pngBytes
            };
        }
        catch (Exception ex)
        {
            DebugLog.Write($"Generate error: {ex}");
            return new DiagramArtifacts
            {
                MermaidText = mermaidText,
                PngBytes = null,
                ErrorMessage = $"PNG generation failed: {ex.Message}"
            };
        }
    }

    public BitmapSource? CreateBitmapSource(byte[]? pngBytes)
    {
        if (pngBytes is null || pngBytes.Length == 0)
        {
            DebugLog.Write("CreateBitmapSource skipped: pngBytes is null or empty.");
            return null;
        }

        try
        {
            BitmapImage image = new();
            using MemoryStream stream = new(pngBytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            DebugLog.Write($"CreateBitmapSource success: pixel={image.PixelWidth}x{image.PixelHeight}, dpi={image.DpiX}x{image.DpiY}");
            return image;
        }
        catch (Exception ex)
        {
            DebugLog.Write($"CreateBitmapSource error: {ex}");
            throw;
        }
    }

    private static string BuildMermaidText(IReadOnlyList<TableRef> tables, IReadOnlyList<TableRelation> relations)
    {
        List<TableRef> orderedTables = tables
            .OrderBy(t => t.Id.Value, StringComparer.Ordinal)
            .ToList();

        List<TableRelation> orderedRelations = relations
            .OrderBy(r => r.From.Value, StringComparer.Ordinal)
            .ThenBy(r => r.To.Value, StringComparer.Ordinal)
            .ThenBy(r => r.JoinType.ToString(), StringComparer.Ordinal)
            .ToList();

        Dictionary<string, string> labelsById = orderedTables.ToDictionary(
            t => t.Id.Value,
            t => BuildNodeLabel(t),
            StringComparer.Ordinal);

        StringWriter writer = new();
        writer.WriteLine("flowchart LR");

        foreach (TableRef table in orderedTables)
        {
            string nodeId = SanitizeNodeId(table.Id.Value);
            string label = EscapeMermaidLabel(labelsById[table.Id.Value]);
            writer.WriteLine($"  {nodeId}[\"{label}\"]");
        }

        foreach (TableRelation relation in orderedRelations)
        {
            string fromId = SanitizeNodeId(relation.From.Value);
            string toId = SanitizeNodeId(relation.To.Value);
            string joinType = relation.JoinType.ToString();
            writer.WriteLine($"  {fromId} -->|{joinType}| {toId}");
        }

        return writer.ToString().TrimEnd();
    }

    private static byte[] RenderPng(IReadOnlyList<TableRef> tables, IReadOnlyList<TableRelation> relations)
    {
        List<TableRef> orderedTables = tables
            .OrderBy(t => t.Id.Value, StringComparer.Ordinal)
            .ToList();

        List<TableRelation> orderedRelations = relations
            .OrderBy(r => r.From.Value, StringComparer.Ordinal)
            .ThenBy(r => r.To.Value, StringComparer.Ordinal)
            .ThenBy(r => r.JoinType.ToString(), StringComparer.Ordinal)
            .ToList();

        const int nodeWidth = 180;
        const int nodeHeight = 60;
        const int margin = 36;
        const int xSpacing = 80;
        const int ySpacing = 100;
        const int maxColumns = 4;

        int nodeCount = Math.Max(orderedTables.Count, 1);
        int columns = Math.Min(maxColumns, nodeCount);
        int rows = (int)Math.Ceiling(nodeCount / (double)columns);

        int width = margin * 2 + columns * nodeWidth + Math.Max(0, columns - 1) * xSpacing;
        int height = margin * 2 + rows * nodeHeight + Math.Max(0, rows - 1) * ySpacing;
        width = Math.Max(width, 420);
        height = Math.Max(height, 220);

        Dictionary<string, Rect> tableRects = new(StringComparer.Ordinal);
        for (int i = 0; i < orderedTables.Count; i++)
        {
            int row = i / columns;
            int col = i % columns;
            double x = margin + col * (nodeWidth + xSpacing);
            double y = margin + row * (nodeHeight + ySpacing);
            tableRects[orderedTables[i].Id.Value] = new Rect(x, y, nodeWidth, nodeHeight);
        }

        DrawingVisual visual = new();
        using (DrawingContext dc = visual.RenderOpen())
        {
            DrawBackground(dc, width, height);

            foreach (TableRelation relation in orderedRelations)
            {
                if (!tableRects.TryGetValue(relation.From.Value, out Rect fromRect) ||
                    !tableRects.TryGetValue(relation.To.Value, out Rect toRect))
                {
                    continue;
                }

                DrawEdge(dc, fromRect, toRect, relation.JoinType.ToString());
            }

            foreach (TableRef table in orderedTables)
            {
                if (!tableRects.TryGetValue(table.Id.Value, out Rect rect))
                {
                    continue;
                }

                DrawNode(dc, rect, BuildNodeLabel(table));
            }
        }

        RenderTargetBitmap bitmap = new(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        LogBitmapStats(bitmap, width, height, orderedTables.Count, orderedRelations.Count);

        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using MemoryStream stream = new();
        encoder.Save(stream);
        DebugLog.Write($"RenderPng encoded: width={width}, height={height}, bytes={stream.Length}");
        return stream.ToArray();
    }

    private static void LogBitmapStats(BitmapSource bitmap, int width, int height, int tableCount, int relationCount)
    {
        try
        {
            int stride = width * 4;
            byte[] pixels = new byte[stride * height];
            bitmap.CopyPixels(pixels, stride, 0);

            long nonTransparent = 0;
            long nonBlack = 0;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                byte a = pixels[i + 3];
                if (a > 0)
                {
                    nonTransparent++;
                }

                if (r > 0 || g > 0 || b > 0)
                {
                    nonBlack++;
                }
            }

            int cx = Math.Max(0, Math.Min(width - 1, width / 2));
            int cy = Math.Max(0, Math.Min(height - 1, height / 2));
            int centerIndex = cy * stride + cx * 4;
            byte cb = pixels[centerIndex];
            byte cg = pixels[centerIndex + 1];
            byte cr = pixels[centerIndex + 2];
            byte ca = pixels[centerIndex + 3];

            DebugLog.Write(
                $"RenderPng stats: tables={tableCount}, relations={relationCount}, px={width}x{height}, " +
                $"nonTransparent={nonTransparent}, nonBlack={nonBlack}, centerBGRA=({cb},{cg},{cr},{ca})");
        }
        catch (Exception ex)
        {
            DebugLog.Write($"RenderPng stats error: {ex}");
        }
    }

    private static void DrawBackground(DrawingContext dc, int width, int height)
    {
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(250, 251, 253)), null, new Rect(0, 0, width, height));
    }

    private static void DrawNode(DrawingContext dc, Rect rect, string text)
    {
        Brush fill = new SolidColorBrush(Color.FromRgb(232, 240, 250));
        Pen border = new(new SolidColorBrush(Color.FromRgb(67, 96, 139)), 1.5);
        dc.DrawRoundedRectangle(fill, border, rect, 8, 8);

        FormattedText formatted = CreateText(text, 14, FontWeights.SemiBold, Brushes.Black);
        Point point = new(
            rect.X + (rect.Width - formatted.Width) / 2,
            rect.Y + (rect.Height - formatted.Height) / 2);
        dc.DrawText(formatted, point);
    }

    private static void DrawEdge(DrawingContext dc, Rect from, Rect to, string label)
    {
        Point start = new(from.Right, from.Top + from.Height / 2);
        Point end = new(to.Left, to.Top + to.Height / 2);
        Pen pen = new(new SolidColorBrush(Color.FromRgb(60, 60, 60)), 1.4);
        dc.DrawLine(pen, start, end);
        DrawArrowHead(dc, start, end);

        Point mid = new((start.X + end.X) / 2, (start.Y + end.Y) / 2 - 14);
        FormattedText text = CreateText(label, 12, FontWeights.Normal, Brushes.Black);
        Rect bg = new(mid.X - 4, mid.Y - 2, text.Width + 8, text.Height + 4);
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(255, 255, 255)), new Pen(new SolidColorBrush(Color.FromRgb(210, 210, 210)), 0.5), bg);
        dc.DrawText(text, new Point(mid.X, mid.Y));
    }

    private static void DrawArrowHead(DrawingContext dc, Point start, Point end)
    {
        Vector direction = end - start;
        if (direction.Length <= 0.1)
        {
            return;
        }

        direction.Normalize();
        Vector perpendicular = new(-direction.Y, direction.X);

        Point tip = end;
        Point left = tip - direction * 10 + perpendicular * 5;
        Point right = tip - direction * 10 - perpendicular * 5;

        StreamGeometry geometry = new();
        using StreamGeometryContext ctx = geometry.Open();
        ctx.BeginFigure(tip, true, true);
        ctx.LineTo(left, true, false);
        ctx.LineTo(right, true, false);
        geometry.Freeze();

        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(60, 60, 60)), null, geometry);
    }

    private static FormattedText CreateText(string text, double size, FontWeight weight, Brush brush)
    {
        return new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            size,
            brush,
            1.0);
    }

    private static string BuildNodeLabel(TableRef table)
    {
        if (!string.IsNullOrWhiteSpace(table.Alias))
        {
            return table.Alias;
        }

        if (!string.IsNullOrWhiteSpace(table.Source.Name?.Object))
        {
            return table.Source.Name.Object;
        }

        if (!string.IsNullOrWhiteSpace(table.Source.ExpressionText))
        {
            return table.Source.ExpressionText;
        }

        return table.Id.Value;
    }

    private static string SanitizeNodeId(string id)
    {
        Span<char> buffer = stackalloc char[id.Length];
        int index = 0;
        foreach (char c in id)
        {
            buffer[index++] = char.IsLetterOrDigit(c) ? c : '_';
        }

        string sanitized = new(buffer[..index]);
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "node";
        }

        if (char.IsDigit(sanitized[0]))
        {
            return $"n_{sanitized}";
        }

        return sanitized;
    }

    private static string EscapeMermaidLabel(string label)
    {
        return label.Replace("\\", "\\\\", StringComparison.Ordinal)
                    .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}

public sealed record DiagramArtifacts
{
    public string MermaidText { get; init; } = string.Empty;

    public byte[]? PngBytes { get; init; }

    public string? ErrorMessage { get; init; }
}
