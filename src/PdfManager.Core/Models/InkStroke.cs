namespace PdfManager.Core.Models;

public sealed class InkStroke
{
    public string Id { get; init; } = $"GPDF-{Guid.NewGuid():N}";
    public int PageIndex { get; init; }
    public byte ColorR { get; init; } = 0;
    public byte ColorG { get; init; } = 0;
    public byte ColorB { get; init; } = 0;
    public double WidthPt { get; init; } = 1.5;
    public List<List<PointPt>> Polylines { get; init; } = new();
    public bool IsForeign { get; init; }

    public RectPt BoundingBox()
    {
        if (Polylines.Count == 0 || Polylines[0].Count == 0)
            return new RectPt(0, 0, 0, 0);

        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;
        foreach (var poly in Polylines)
            foreach (var p in poly)
            {
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }
        var pad = WidthPt;
        return new RectPt(minX - pad, minY - pad, maxX + pad, maxY + pad);
    }
}

public readonly record struct PointPt(double X, double Y);

public readonly record struct RectPt(double Left, double Bottom, double Right, double Top)
{
    public double Width => Right - Left;
    public double Height => Top - Bottom;
}
