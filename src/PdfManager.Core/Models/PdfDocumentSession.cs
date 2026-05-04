namespace PdfManager.Core.Models;

public sealed class PdfDocumentSession
{
    public required string FilePath { get; set; }
    public string? Password { get; set; }
    public bool IsDirty { get; set; }
    public int PageCount { get; set; }
    public List<InkStroke> Strokes { get; } = new();
    public List<FreeTextNote> Notes { get; } = new();

    public IEnumerable<InkStroke> StrokesOnPage(int pageIndex) =>
        Strokes.Where(s => s.PageIndex == pageIndex);

    public IEnumerable<FreeTextNote> NotesOnPage(int pageIndex) =>
        Notes.Where(n => n.PageIndex == pageIndex);
}
