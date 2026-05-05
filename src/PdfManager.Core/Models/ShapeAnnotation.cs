namespace PdfManager.Core.Models;

public enum ShapeKind { Arrow, Circle }

public sealed class ShapeAnnotation
{
    public string Id { get; init; } = $"GPDF-{Guid.NewGuid():N}";
    public int PageIndex { get; init; }
    public ShapeKind Kind { get; init; }
    public PointPt Start { get; init; }
    public PointPt End   { get; init; }
    public byte ColorR { get; init; }
    public byte ColorG { get; init; }
    public byte ColorB { get; init; }
    public double WidthPt { get; init; } = 2;
}
