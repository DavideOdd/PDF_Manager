using PdfManager.Core.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfManager.Core.Services;

public sealed class PdfSharpDocumentService : IPdfDocumentService
{
    private readonly IAnnotationWriter _writer;

    public PdfSharpDocumentService(IAnnotationWriter writer)
    {
        PdfSharpInit.EnsureRegistered();
        _writer = writer;
    }

    public int GetPageCount(string path, string? password = null)
    {
        using var doc = OpenForRead(path, password);
        return doc.PageCount;
    }

    public void MovePage(string srcPath, int from, int to, string outPath, string? password = null)
    {
        using var src = OpenForImport(srcPath, password);
        int count = src.PageCount;
        if (from < 0 || from >= count) throw new ArgumentOutOfRangeException(nameof(from));
        if (to < 0 || to >= count) throw new ArgumentOutOfRangeException(nameof(to));
        var order = Enumerable.Range(0, count).ToList();
        order.RemoveAt(from);
        order.Insert(to, from);
        using var outDoc = new PdfDocument();
        foreach (var idx in order) outDoc.AddPage(src.Pages[idx]);
        outDoc.Save(outPath);
    }

    public void RotatePage(string srcPath, int pageIndex, int deltaDeg, string outPath, string? password = null)
    {
        using var doc = OpenForModify(srcPath, password);
        var p = doc.Pages[pageIndex];
        p.Rotate = (p.Rotate + deltaDeg) % 360;
        if (p.Rotate < 0) p.Rotate += 360;
        doc.Save(outPath);
    }

    public void DeletePage(string srcPath, int pageIndex, string outPath, string? password = null)
    {
        using var doc = OpenForModify(srcPath, password);
        doc.Pages.RemoveAt(pageIndex);
        doc.Save(outPath);
    }

    public void Split(string srcPath, IReadOnlyList<int> pageIndexes, string outPath, string? password = null)
    {
        using var src = OpenForImport(srcPath, password);
        using var outDoc = new PdfDocument();
        foreach (var idx in pageIndexes) outDoc.AddPage(src.Pages[idx]);
        outDoc.Save(outPath);
    }

    public void Combine(IReadOnlyList<CombineItem> items, string outPath)
    {
        using var output = new PdfDocument();
        foreach (var item in items)
        {
            if (item.Kind == CombineItemKind.Pdf)
            {
                using var src = OpenForImport(item.Path, password: null);
                foreach (var p in src.Pages)
                {
                    var added = output.AddPage(p);
                    added.Rotate = (added.Rotate + item.RotationDeg) % 360;
                    if (added.Rotate < 0) added.Rotate += 360;
                }
            }
            else
            {
                AddImagePage(output, item);
            }
        }
        output.Save(outPath);
    }

    private static void AddImagePage(PdfDocument output, CombineItem item)
    {
        var page = output.AddPage();
        using var img = XImage.FromFile(item.Path);
        double targetWPt = img.PointWidth > 0 ? img.PointWidth : img.PixelWidth * 72.0 / Math.Max(img.HorizontalResolution, 1);
        double targetHPt = img.PointHeight > 0 ? img.PointHeight : img.PixelHeight * 72.0 / Math.Max(img.VerticalResolution, 1);
        page.Width = XUnit.FromPoint(targetWPt);
        page.Height = XUnit.FromPoint(targetHPt);
        using var gfx = XGraphics.FromPdfPage(page);
        gfx.DrawImage(img, 0, 0, targetWPt, targetHPt);
        if (item.RotationDeg != 0)
        {
            page.Rotate = ((item.RotationDeg % 360) + 360) % 360;
        }
    }

    public string ImageToTempPdf(string imagePath)
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"GestorePDF_{Guid.NewGuid():N}.pdf");
        using var doc  = new PdfDocument();
        using var img  = XImage.FromFile(imagePath);
        double wPt = img.PointWidth  > 0 ? img.PointWidth  : img.PixelWidth  * 72.0 / Math.Max(img.HorizontalResolution, 1);
        double hPt = img.PointHeight > 0 ? img.PointHeight : img.PixelHeight * 72.0 / Math.Max(img.VerticalResolution, 1);
        if (wPt <= 0) wPt = img.PixelWidth  * 72.0 / 96.0;
        if (hPt <= 0) hPt = img.PixelHeight * 72.0 / 96.0;
        var page = doc.AddPage();
        page.Width  = XUnit.FromPoint(wPt);
        page.Height = XUnit.FromPoint(hPt);
        using var gfx = XGraphics.FromPdfPage(page);
        gfx.DrawImage(img, 0, 0, wPt, hPt);
        doc.Save(tmp);
        return tmp;
    }

    public void SaveWithAnnotations(
        string srcPath,
        string outPath,
        IReadOnlyList<InkStroke> inkStrokes,
        IReadOnlyList<FreeTextNote> freeTextNotes,
        string? password = null)
    {
        using var doc = OpenForModify(srcPath, password);

        for (int i = 0; i < doc.PageCount; i++)
            _writer.RemoveOwnAnnotations(doc.Pages[i]);

        foreach (var s in inkStrokes)
        {
            if (s.IsForeign) continue;
            if (s.PageIndex < 0 || s.PageIndex >= doc.PageCount) continue;
            _writer.WriteInk(doc.Pages[s.PageIndex], s);
        }
        foreach (var n in freeTextNotes)
        {
            if (n.IsForeign) continue;
            if (n.PageIndex < 0 || n.PageIndex >= doc.PageCount) continue;
            _writer.WriteFreeText(doc.Pages[n.PageIndex], n);
        }

        doc.Save(outPath);
    }

    private static PdfDocument OpenForRead(string path, string? password)
    {
        PdfSharpInit.EnsureRegistered();
        return password is null
            ? PdfReader.Open(path, PdfDocumentOpenMode.Import)
            : PdfReader.Open(path, password, PdfDocumentOpenMode.Import);
    }

    private static PdfDocument OpenForModify(string path, string? password)
    {
        PdfSharpInit.EnsureRegistered();
        return password is null
            ? PdfReader.Open(path, PdfDocumentOpenMode.Modify)
            : PdfReader.Open(path, password, PdfDocumentOpenMode.Modify);
    }

    private static PdfDocument OpenForImport(string path, string? password)
    {
        PdfSharpInit.EnsureRegistered();
        return password is null
            ? PdfReader.Open(path, PdfDocumentOpenMode.Import)
            : PdfReader.Open(path, password, PdfDocumentOpenMode.Import);
    }
}
