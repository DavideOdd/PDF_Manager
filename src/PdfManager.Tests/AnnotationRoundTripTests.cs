using FluentAssertions;
using PdfManager.Core.Models;
using PdfManager.Core.Services;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PdfManager.Tests;

public sealed class AnnotationRoundTripTests : IDisposable
{
    private readonly string _tempDir;
    private readonly PdfAnnotationWriter _writer = new();
    private readonly PdfAnnotationReader _reader = new();

    public AnnotationRoundTripTests()
    {
        PdfSharpInit.EnsureRegistered();
        _tempDir = Path.Combine(Path.GetTempPath(), $"GestorePDF_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void WriteInk_Roundtrip_PointsPreserved()
    {
        var path = CreateMinimalPdf();

        var stroke = new InkStroke
        {
            PageIndex = 0,
            ColorR = 255, ColorG = 0, ColorB = 0,
            WidthPt = 3,
            Polylines = new List<List<PointPt>>
            {
                new List<PointPt> { new(10, 20), new(30, 40), new(50, 60) }
            }
        };

        using (var doc = PdfReader.Open(path, PdfDocumentOpenMode.Modify))
        {
            _writer.WriteInk(doc.Pages[0], stroke);
            doc.Save(path);
        }

        using (var doc = PdfReader.Open(path, PdfDocumentOpenMode.Import))
        {
            var strokes = _reader.ReadInk(doc.Pages[0], 0);
            strokes.Should().HaveCount(1);
            var s = strokes[0];
            s.ColorR.Should().Be(255);
            s.ColorG.Should().Be(0);
            s.Polylines.Should().HaveCount(1);
            s.Polylines[0].Should().HaveCount(3);
            s.Polylines[0][0].X.Should().BeApproximately(10, 0.01);
            s.Polylines[0][0].Y.Should().BeApproximately(20, 0.01);
        }
    }

    [Fact]
    public void WriteFiveStrokes_Roundtrip_AllPreserved()
    {
        var path = CreateMinimalPdf();

        using (var doc = PdfReader.Open(path, PdfDocumentOpenMode.Modify))
        {
            for (int i = 0; i < 5; i++)
            {
                var stroke = new InkStroke
                {
                    PageIndex = 0,
                    ColorR = (byte)(i * 40),
                    WidthPt = i + 1,
                    Polylines = new List<List<PointPt>>
                        { new List<PointPt> { new(i * 10, i * 5), new(i * 10 + 5, i * 5 + 5) } }
                };
                _writer.WriteInk(doc.Pages[0], stroke);
            }
            doc.Save(path);
        }

        using (var doc = PdfReader.Open(path, PdfDocumentOpenMode.Import))
        {
            var strokes = _reader.ReadInk(doc.Pages[0], 0);
            strokes.Should().HaveCount(5);
        }
    }

    [Fact]
    public void WriteFreeText_Roundtrip_ContentsPreserved()
    {
        var path = CreateMinimalPdf();
        const string expected = "Ciao Mondo 🌍";

        using (var doc = PdfReader.Open(path, PdfDocumentOpenMode.Modify))
        {
            _writer.WriteFreeText(doc.Pages[0], new FreeTextNote
            {
                PageIndex = 0,
                Text = expected,
                Rect = new RectPt(10, 20, 200, 50),
                FontSizePt = 14
            });
            doc.Save(path);
        }

        using (var doc = PdfReader.Open(path, PdfDocumentOpenMode.Import))
        {
            var notes = _reader.ReadFreeText(doc.Pages[0], 0);
            notes.Should().HaveCount(1);
            notes[0].Text.Should().Be(expected);
        }
    }

    [Fact]
    public void ForeignAnnotations_Preserved_OnResave()
    {
        // Create PDF with a foreign (non-GPDF) ink annotation
        var path = CreateMinimalPdf();
        using (var doc = PdfReader.Open(path, PdfDocumentOpenMode.Modify))
        {
            // Write as foreign: no GPDF- prefix in /NM
            var annot = new PdfDictionary(doc.Pages[0].Owner);
            annot.Elements.SetName("/Type", "/Annot");
            annot.Elements.SetName("/Subtype", "/Ink");
            annot.Elements.SetInteger("/F", 4);
            annot.Elements.SetString("/NM", "ACROBAT-1234");
            annot.Elements["/Rect"] = new PdfArray(doc.Pages[0].Owner,
                new PdfReal(0), new PdfReal(0), new PdfReal(10), new PdfReal(10));
            var inkList = new PdfArray(doc.Pages[0].Owner);
            var pts = new PdfArray(doc.Pages[0].Owner, new PdfReal(1), new PdfReal(1));
            inkList.Elements.Add(pts);
            annot.Elements["/InkList"] = inkList;
            doc.Pages[0].Owner.Internals.AddObject(annot);
            var iref = PdfSharp.Pdf.Advanced.PdfInternals.GetReference(annot);
            var annots = new PdfArray(doc.Pages[0].Owner);
            annots.Elements.Add(iref);
            doc.Pages[0].Elements["/Annots"] = annots;
            doc.Save(path);
        }

        // Now add our own stroke and save
        using (var doc = PdfReader.Open(path, PdfDocumentOpenMode.Modify))
        {
            _writer.RemoveOwnAnnotations(doc.Pages[0]);
            _writer.WriteInk(doc.Pages[0], new InkStroke
            {
                PageIndex = 0, WidthPt = 1,
                Polylines = new List<List<PointPt>> { new List<PointPt> { new(5, 5), new(10, 10) } }
            });
            doc.Save(path);
        }

        // Foreign annotation must still be there
        using (var doc = PdfReader.Open(path, PdfDocumentOpenMode.Import))
        {
            var annots = doc.Pages[0].Elements.GetArray("/Annots");
            annots.Should().NotBeNull();
            // 1 foreign + 1 ours = 2
            annots!.Elements.Count.Should().Be(2);
            var nms = annots.Elements
                .Select(x => PdfAnnotationWriter.ResolveDictionary(x)?.Elements.GetString("/NM") ?? "")
                .ToList();
            nms.Should().Contain("ACROBAT-1234");
        }
    }

    [Fact]
    public void RemoveOwnAnnotations_LeavesOnlyForeign()
    {
        var path = CreateMinimalPdf();
        using (var doc = PdfReader.Open(path, PdfDocumentOpenMode.Modify))
        {
            _writer.WriteInk(doc.Pages[0], new InkStroke
            {
                PageIndex = 0, WidthPt = 1,
                Polylines = new List<List<PointPt>> { new List<PointPt> { new(1, 1), new(2, 2) } }
            });
            _writer.RemoveOwnAnnotations(doc.Pages[0]);
            doc.Save(path);
        }
        using (var doc = PdfReader.Open(path, PdfDocumentOpenMode.Import))
        {
            var strokes = _reader.ReadInk(doc.Pages[0], 0);
            strokes.Should().BeEmpty();
        }
    }

    private string CreateMinimalPdf()
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}.pdf");
        using var doc = new PdfDocument();
        doc.AddPage();
        doc.Save(path);
        return path;
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}
