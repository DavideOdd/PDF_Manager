using PdfManager.Core.Models;

namespace PdfManager.Core.Services;

public interface IPdfDocumentService
{
    int GetPageCount(string path, string? password = null);
    void MovePage(string srcPath, int from, int to, string outPath, string? password = null);
    void RotatePage(string srcPath, int pageIndex, int deltaDeg, string outPath, string? password = null);
    void DeletePage(string srcPath, int pageIndex, string outPath, string? password = null);
    void Split(string srcPath, IReadOnlyList<int> pageIndexes, string outPath, string? password = null);
    void Combine(IReadOnlyList<CombineItem> items, string outPath);
    string ImageToTempPdf(string imagePath);
    void CreateBlankDocument(string outPath, double widthPt, double heightPt);
    void SaveWithAnnotations(
        string srcPath,
        string outPath,
        IReadOnlyList<InkStroke> inkStrokes,
        IReadOnlyList<FreeTextNote> freeTextNotes,
        string? password = null);
}
