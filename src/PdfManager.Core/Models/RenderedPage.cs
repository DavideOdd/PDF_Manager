namespace PdfManager.Core.Models;

public sealed class RenderedPage
{
    public required int PageIndex { get; init; }
    public required int PixelWidth { get; init; }
    public required int PixelHeight { get; init; }
    public required double PageWidthPt { get; init; }
    public required double PageHeightPt { get; init; }
    public required double Dpi { get; init; }
    public required byte[] BgraBuffer { get; init; }
    public required int Stride { get; init; }
}
