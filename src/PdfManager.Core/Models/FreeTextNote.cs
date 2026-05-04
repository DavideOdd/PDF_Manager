namespace PdfManager.Core.Models;

public sealed class FreeTextNote
{
    public string Id { get; init; } = $"GPDF-{Guid.NewGuid():N}";
    public int PageIndex { get; init; }
    public string Text { get; set; } = string.Empty;
    public RectPt Rect { get; set; }
    public byte ColorR { get; init; } = 0;
    public byte ColorG { get; init; } = 0;
    public byte ColorB { get; init; } = 0;
    public double FontSizePt { get; init; } = 12;
    public bool IsForeign { get; init; }
}
