using FluentAssertions;
using PdfManager.Core.Models;
using PdfManager.Core.Services;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfManager.Tests;

public sealed class MergeTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PdfSharpDocumentService _service;

    public MergeTests()
    {
        PdfSharpInit.EnsureRegistered();
        _tempDir = Path.Combine(Path.GetTempPath(), $"GestorePDF_Merge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _service = new PdfSharpDocumentService(new PdfAnnotationWriter());
    }

    [Fact]
    public void Combine_TwoPdfs_OutputHasCorrectPageCount()
    {
        var a = CreatePdfWithPages(2);
        var b = CreatePdfWithPages(3);
        var outPath = Path.Combine(_tempDir, "combined.pdf");

        _service.Combine(new[]
        {
            new CombineItem { Path = a, Kind = CombineItemKind.Pdf },
            new CombineItem { Path = b, Kind = CombineItemKind.Pdf }
        }, outPath);

        using var doc = PdfReader.Open(outPath, PdfDocumentOpenMode.Import);
        doc.PageCount.Should().Be(5);
    }

    [Fact]
    public void Combine_PdfAndImages_OutputHasCorrectPageCount()
    {
        var pdf = CreatePdfWithPages(2);
        var img1 = CreateTestImage();
        var img2 = CreateTestImage();
        var outPath = Path.Combine(_tempDir, "combined_img.pdf");

        _service.Combine(new[]
        {
            new CombineItem { Path = pdf,  Kind = CombineItemKind.Pdf   },
            new CombineItem { Path = img1, Kind = CombineItemKind.Image },
            new CombineItem { Path = img2, Kind = CombineItemKind.Image }
        }, outPath);

        using var doc = PdfReader.Open(outPath, PdfDocumentOpenMode.Import);
        doc.PageCount.Should().Be(4);
    }

    [Fact]
    public void Split_ExtractsSpecifiedPages()
    {
        var src = CreatePdfWithPages(5);
        var outPath = Path.Combine(_tempDir, "split.pdf");

        _service.Split(src, new[] { 0, 2, 4 }, outPath);

        using var doc = PdfReader.Open(outPath, PdfDocumentOpenMode.Import);
        doc.PageCount.Should().Be(3);
    }

    private string CreatePdfWithPages(int count)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.pdf");
        using var doc = new PdfDocument();
        for (int i = 0; i < count; i++) doc.AddPage();
        doc.Save(path);
        return path;
    }

    private string CreateTestImage()
    {
        // Minimal valid 1x1 white pixel PNG (67 bytes)
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.png");
        // Produced by: python3 -c "import struct,zlib; ..."
        byte[] png = new byte[]
        {
            0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A, // PNG signature
            0x00,0x00,0x00,0x0D,                       // IHDR length
            0x49,0x48,0x44,0x52,                       // IHDR
            0x00,0x00,0x00,0x01,                       // width=1
            0x00,0x00,0x00,0x01,                       // height=1
            0x08,0x02,                                 // 8-bit RGB
            0x00,0x00,0x00,
            0x90,0x77,0x53,0xDE,                       // CRC
            0x00,0x00,0x00,0x0C,                       // IDAT length
            0x49,0x44,0x41,0x54,                       // IDAT
            0x08,0xD7,0x63,0xF8,0xFF,0xFF,0x3F,0x00,  // compressed pixel
            0x05,0xFE,0x02,0xFE,
            0xDC,0xCC,0x59,0xE7,                       // CRC
            0x00,0x00,0x00,0x00,                       // IEND length
            0x49,0x45,0x4E,0x44,                       // IEND
            0xAE,0x42,0x60,0x82                        // CRC
        };
        File.WriteAllBytes(path, png);
        return path;
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
