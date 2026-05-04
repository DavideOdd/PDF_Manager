using FluentAssertions;
using PdfManager.Core.Services;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfManager.Tests;

public sealed class PageOpsTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PdfSharpDocumentService _service;

    public PageOpsTests()
    {
        PdfSharpInit.EnsureRegistered();
        _tempDir = Path.Combine(Path.GetTempPath(), $"GestorePDF_Pages_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _service = new PdfSharpDocumentService(new PdfAnnotationWriter());
    }

    [Fact]
    public void DeletePage_ReducesCount()
    {
        var path = CreatePdf(3);
        var outPath = Path.Combine(_tempDir, "del.pdf");
        _service.DeletePage(path, 1, outPath);
        using var doc = PdfReader.Open(outPath, PdfDocumentOpenMode.Import);
        doc.PageCount.Should().Be(2);
    }

    [Fact]
    public void RotatePage_PersistsRotation()
    {
        var path = CreatePdf(1);
        var outPath = Path.Combine(_tempDir, "rot.pdf");
        _service.RotatePage(path, 0, 90, outPath);
        using var doc = PdfReader.Open(outPath, PdfDocumentOpenMode.Import);
        doc.Pages[0].Rotate.Should().Be(90);
    }

    [Fact]
    public void MovePage_ChangesOrder()
    {
        // Create a 3-page PDF where we can distinguish pages by size
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.pdf");
        using (var doc = new PdfDocument())
        {
            var p0 = doc.AddPage(); p0.Width = PdfSharp.Drawing.XUnit.FromPoint(100);
            var p1 = doc.AddPage(); p1.Width = PdfSharp.Drawing.XUnit.FromPoint(200);
            var p2 = doc.AddPage(); p2.Width = PdfSharp.Drawing.XUnit.FromPoint(300);
            doc.Save(path);
        }
        var outPath = Path.Combine(_tempDir, "moved.pdf");
        _service.MovePage(path, 0, 2, outPath);
        using var result = PdfReader.Open(outPath, PdfDocumentOpenMode.Import);
        // page 0 was moved to 2, so old page1(200) is now page0
        result.Pages[0].Width.Point.Should().BeApproximately(200, 0.1);
        result.Pages[2].Width.Point.Should().BeApproximately(100, 0.1);
    }

    private string CreatePdf(int pages)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.pdf");
        using var doc = new PdfDocument();
        for (int i = 0; i < pages; i++) doc.AddPage();
        doc.Save(path);
        return path;
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
