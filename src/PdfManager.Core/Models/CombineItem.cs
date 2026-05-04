namespace PdfManager.Core.Models;

public enum CombineItemKind { Pdf, Image }

public sealed class CombineItem
{
    public required string Path { get; init; }
    public required CombineItemKind Kind { get; init; }
    public int RotationDeg { get; set; }
    public string DisplayName => System.IO.Path.GetFileName(Path);
}
