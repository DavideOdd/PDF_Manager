using System.Globalization;
using PdfManager.Core.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;

namespace PdfManager.Core.Services;

public sealed class PdfAnnotationWriter : IAnnotationWriter
{
    public const string OwnNamePrefix = "GPDF-";

    public void WriteInk(PdfPage page, InkStroke stroke)
    {
        var doc = page.Owner;
        var annot = new PdfDictionary(doc);
        annot.Elements.SetName("/Type", "/Annot");
        annot.Elements.SetName("/Subtype", "/Ink");
        annot.Elements.SetInteger("/F", 4);
        annot.Elements.SetString("/NM", EnsureOwnId(stroke.Id));
        annot.Elements.SetString("/M", $"D:{DateTime.UtcNow:yyyyMMddHHmmss}Z");
        annot.Elements.SetString("/T", "Gestore PDF");

        var bb = stroke.BoundingBox();
        annot.Elements["/Rect"] = MakeArray(doc, bb.Left, bb.Bottom, bb.Right, bb.Top);

        annot.Elements["/C"] = MakeArray(doc, stroke.ColorR / 255.0, stroke.ColorG / 255.0, stroke.ColorB / 255.0);

        var bs = new PdfDictionary(doc);
        bs.Elements.SetName("/Type", "/Border");
        bs.Elements.SetReal("/W", stroke.WidthPt);
        bs.Elements.SetName("/S", "/S");
        annot.Elements["/BS"] = bs;

        var inkList = new PdfArray(doc);
        foreach (var poly in stroke.Polylines)
        {
            var pts = new PdfArray(doc);
            foreach (var p in poly)
            {
                pts.Elements.Add(new PdfReal(p.X));
                pts.Elements.Add(new PdfReal(p.Y));
            }
            inkList.Elements.Add(pts);
        }
        annot.Elements["/InkList"] = inkList;

        AppendAnnotation(page, annot);
    }

    public void WriteFreeText(PdfPage page, FreeTextNote note)
    {
        var doc = page.Owner;
        var annot = new PdfDictionary(doc);
        annot.Elements.SetName("/Type", "/Annot");
        annot.Elements.SetName("/Subtype", "/FreeText");
        annot.Elements.SetInteger("/F", 4);
        annot.Elements.SetString("/NM", EnsureOwnId(note.Id));
        annot.Elements.SetString("/M", $"D:{DateTime.UtcNow:yyyyMMddHHmmss}Z");
        annot.Elements.SetString("/T", "Gestore PDF");
        annot.Elements.SetString("/Contents", note.Text);

        annot.Elements["/Rect"] = MakeArray(doc,
            note.Rect.Left, note.Rect.Bottom, note.Rect.Right, note.Rect.Top);

        var rgb = string.Format(CultureInfo.InvariantCulture, "{0:0.###} {1:0.###} {2:0.###} rg /Helv {3:0.##} Tf",
            note.ColorR / 255.0, note.ColorG / 255.0, note.ColorB / 255.0, note.FontSizePt);
        annot.Elements.SetString("/DA", rgb);
        annot.Elements.SetInteger("/Q", 0);

        AppendAnnotation(page, annot);
    }

    public void RemoveOwnAnnotations(PdfPage page)
    {
        var annots = page.Elements.GetArray("/Annots");
        if (annots == null || annots.Elements.Count == 0) return;

        var keep = new List<PdfItem>();
        foreach (var item in annots.Elements)
        {
            var dict = ResolveDictionary(item);
            if (dict == null) { keep.Add(item); continue; }
            var nm = dict.Elements.GetString("/NM");
            if (!string.IsNullOrEmpty(nm) && nm.StartsWith(OwnNamePrefix, StringComparison.Ordinal))
                continue;
            keep.Add(item);
        }
        annots.Elements.Clear();
        foreach (var k in keep) annots.Elements.Add(k);
    }

    private static void AppendAnnotation(PdfPage page, PdfDictionary annot)
    {
        var doc = page.Owner;
        doc.Internals.AddObject(annot);
        var iref = PdfInternals.GetReference(annot);
        var annots = page.Elements.GetArray("/Annots");
        if (annots == null)
        {
            annots = new PdfArray(doc);
            page.Elements["/Annots"] = annots;
        }
        annots.Elements.Add(iref);
    }

    private static PdfArray MakeArray(PdfDocument doc, params double[] values)
    {
        var arr = new PdfArray(doc);
        foreach (var v in values) arr.Elements.Add(new PdfReal(v));
        return arr;
    }

    private static string EnsureOwnId(string id) =>
        id.StartsWith(OwnNamePrefix, StringComparison.Ordinal) ? id : $"{OwnNamePrefix}{id}";

    public static PdfDictionary? ResolveDictionary(PdfItem item) => item switch
    {
        PdfReference r => r.Value as PdfDictionary,
        PdfDictionary d => d,
        _ => null
    };
}
